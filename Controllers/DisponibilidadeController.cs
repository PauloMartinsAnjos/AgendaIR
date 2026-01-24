using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Services;

namespace AgendaIR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisponibilidadeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleCalendarService _calendarService;
        private readonly ILogger<DisponibilidadeController> _logger;

        public DisponibilidadeController(
            ApplicationDbContext context,
            GoogleCalendarService calendarService,
            ILogger<DisponibilidadeController> logger)
        {
            _context = context;
            _calendarService = calendarService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/disponibilidade?funcionarioId=1&data=2026-01-24&duracao=60
        /// Retorna lista de horários disponíveis/ocupados
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObterHorariosDisponiveis(
            int funcionarioId,
            DateTime data,
            int duracao = 60)
        {
            _logger.LogInformation($"🔍 Buscando disponibilidade - Funcionário: {funcionarioId}, Data: {data:dd/MM/yyyy}, Duração: {duracao}min");

            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Id == funcionarioId);

            if (funcionario == null)
            {
                _logger.LogWarning($"❌ Funcionário {funcionarioId} não encontrado");
                return NotFound(new { erro = "Funcionário não encontrado" });
            }

            var horarios = new List<HorarioDisponivel>();
            
            // Horário comercial: 8h às 17h (Horário de Brasília)
            var timeZoneBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var agora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneBrasilia);

            var horaInicioLocal = new DateTime(data.Year, data.Month, data.Day, 8, 0, 0, DateTimeKind.Unspecified);
            var horaFimLocal = new DateTime(data.Year, data.Month, data.Day, 17, 0, 0, DateTimeKind.Unspecified);

            var horaInicio = TimeZoneInfo.ConvertTimeToUtc(horaInicioLocal, timeZoneBrasilia);
            var horaFim = TimeZoneInfo.ConvertTimeToUtc(horaFimLocal, timeZoneBrasilia);

            _logger.LogInformation($"🕐 Horário local: {horaInicioLocal:HH:mm} - {horaFimLocal:HH:mm}");
            _logger.LogInformation($"🕐 Horário UTC: {horaInicio:HH:mm} - {horaFim:HH:mm}");

            // Gerar intervalos de 30 em 30 minutos
            for (var hora = horaInicio; hora < horaFim; hora = hora.AddMinutes(30))
            {
                var horaTermino = hora.AddMinutes(duracao);
                
                // Se passar das 18h, parar
                if (horaTermino > horaFim)
                    break;

                // Verificar se está disponível
                var disponivel = await VerificarDisponibilidade(
                    funcionarioId,
                    funcionario.GoogleCalendarEmail,
                    hora,
                    duracao
                );

                horarios.Add(new HorarioDisponivel
                {
                    Inicio = hora,
                    Fim = horaTermino,
                    Disponivel = disponivel
                });
            }

            _logger.LogInformation($"✅ {horarios.Count} horários verificados - {horarios.Count(h => h.Disponivel)} livres");

            return Ok(horarios);
        }

        /// <summary>
        /// Verifica se funcionário está disponível no horário
        /// Consulta: 1) Agendamentos locais 2) Google Calendar
        /// </summary>
        private async Task<bool> VerificarDisponibilidade(
            int funcionarioId,
            string? googleEmail,
            DateTime inicio,
            int duracao)
        {
            var fim = inicio.AddMinutes(duracao);

            // 1️⃣ VERIFICAR AGENDAMENTOS LOCAIS
            var temAgendamentoLocal = await _context.Agendamentos
                .Where(a => a.FuncionarioId == funcionarioId)
                .Where(a => a.Status != "Cancelado")
                .AnyAsync(a =>
                    // Conflito: novo agendamento começa durante um existente
                    (a.DataHora >= inicio && a.DataHora < fim) ||
                    // Conflito: novo agendamento termina durante um existente
                    (a.DataHora < inicio && a.DataHora.AddMinutes(duracao) > inicio)
                );

            if (temAgendamentoLocal)
            {
                _logger.LogDebug($"❌ {inicio:HH:mm} - OCUPADO (agendamento local)");
                return false;
            }

            // 2️⃣ VERIFICAR GOOGLE CALENDAR (se configurado)
            if (!string.IsNullOrEmpty(googleEmail))
            {
                try
                {
                    var disponivelNoGoogle = await _calendarService
                        .VerificarDisponibilidadeAsync(googleEmail, inicio, duracao);
                    
                    if (!disponivelNoGoogle)
                    {
                        _logger.LogDebug($"❌ {inicio:HH:mm} - OCUPADO (Google Calendar)");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"⚠️ Erro ao verificar Google Calendar - considerando disponível");
                    // Em caso de erro, considera disponível
                    return true;
                }
            }

            _logger.LogDebug($"✅ {inicio:HH:mm} - LIVRE");
            return true;
        }
    }

    /// <summary>
    /// ViewModel para horário disponível
    /// </summary>
    public class HorarioDisponivel
    {
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }
        public bool Disponivel { get; set; }
    }
}
