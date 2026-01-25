using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;
using AgendaIR.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar agendamentos
    /// Implementa funcionalidades diferentes para Clientes, Funcionários e Administradores
    /// </summary>
    [Authorize]
    public class AgendamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleCalendarService _calendarService;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<AgendamentosController> _logger;

        public AgendamentosController(
            ApplicationDbContext context,
            GoogleCalendarService calendarService,
            FileUploadService fileUploadService,
            ILogger<AgendamentosController> logger)
        {
            _context = context;
            _calendarService = calendarService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        #region Métodos Auxiliares

        /// <summary>
        /// Obtém o ID do usuário logado dos Claims
        /// </summary>
        private int? GetUsuarioId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Obtém o tipo de usuário logado (Cliente, Funcionario)
        /// </summary>
        private string? GetUserType()
        {
            return User.FindFirst("UserType")?.Value;
        }

        /// <summary>
        /// Verifica se o usuário logado é admin
        /// </summary>
        private bool IsAdmin()
        {
            return User.FindFirst("IsAdmin")?.Value == "True";
        }

        /// <summary>
        /// Valida se a data/hora do agendamento está dentro das regras de negócio
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidarDataHoraAgendamento(DateTime dataHora)
        {
            // Verificar se a data é futura
            if (dataHora <= DateTime.UtcNow)
            {
                return (false, "A data e hora devem ser futuras");
            }

            // Verificar se é dia útil (segunda a sexta)
            if (dataHora.DayOfWeek == DayOfWeek.Saturday || dataHora.DayOfWeek == DayOfWeek.Sunday)
            {
                return (false, "Agendamentos só podem ser feitos de segunda a sexta-feira");
            }

            // Verificar horário (8h às 18h)
            if (dataHora.Hour < 8 || dataHora.Hour >= 18)
            {
                return (false, "Agendamentos só podem ser feitos entre 8h e 18h");
            }

            return (true, string.Empty);
        }

        #endregion

        #region Ações para CLIENTES VIA LINK MÁGICO (Anonymous)

        /// <summary>
        /// Acessa link mágico - redireciona para lista de agendamentos
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> AcessarLinkMagico(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token não fornecido");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.MagicToken == token);

            if (cliente == null)
            {
                return View("../Auth/TokenInvalido");
            }

            // Validar expiração (8 horas - NÃO MEXER)
            if (!cliente.TokenValido())
            {
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.ClienteId = cliente.Id;
                ViewBag.TokenExpiracao = cliente.TokenExpiracao;
                return View("../Auth/TokenExpirado");
            }

            // Redirecionar para lista de agendamentos
            return RedirectToAction("MeusAgendamentos", new { token = token });
        }

        /// <summary>
        /// Cancelar agendamento (cliente via link mágico)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarMeuAgendamento(int id, string token)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.MagicToken == token);

            if (cliente == null || !cliente.TokenValido())
            {
                return View("../Auth/TokenExpirado");
            }

            var agendamento = await _context.Agendamentos
                .FirstOrDefaultAsync(a => a.Id == id && a.ClienteId == cliente.Id);

            if (agendamento == null)
            {
                return NotFound();
            }

            agendamento.Status = "Cancelado";
            agendamento.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Mensagem"] = "✅ Agendamento cancelado com sucesso!";
            return RedirectToAction("MeusAgendamentos", new { token = token });
        }

        /// <summary>
        /// Editar agendamento (cliente via link mágico)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EditarMeuAgendamento(int id, string token)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.MagicToken == token);

            if (cliente == null || !cliente.TokenValido())
            {
                return View("../Auth/TokenExpirado");
            }

            var agendamento = await _context.Agendamentos
                .Include(a => a.TipoAgendamento)
                .Include(a => a.Funcionario)
                .FirstOrDefaultAsync(a => a.Id == id && a.ClienteId == cliente.Id);

            if (agendamento == null)
            {
                return NotFound();
            }

            ViewBag.Token = token;
            return View(agendamento);
        }

        /// <summary>
        /// Salvar edição do agendamento (cliente via link mágico)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMeuAgendamento(int id, string token, Agendamento model)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.MagicToken == token);

            if (cliente == null || !cliente.TokenValido())
            {
                return View("../Auth/TokenExpirado");
            }

            var agendamento = await _context.Agendamentos
                .FirstOrDefaultAsync(a => a.Id == id && a.ClienteId == cliente.Id);

            if (agendamento == null)
            {
                return NotFound();
            }

            // Atualizar apenas DataHora e Observações
            agendamento.DataHora = model.DataHora;
            agendamento.Observacoes = model.Observacoes;
            agendamento.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Atualizar Google Calendar
            var funcionario = await _context.Funcionarios.FindAsync(agendamento.FuncionarioId);
            var tipoAgendamento = await _context.TiposAgendamento.FindAsync(agendamento.TipoAgendamentoId);

            if (funcionario?.GoogleCalendarEmail != null && !string.IsNullOrEmpty(agendamento.GoogleCalendarEventId))
            {
                try
                {
                    await _calendarService.AtualizarEventoAsync(
                        funcionario.GoogleCalendarEmail,
                        agendamento.GoogleCalendarEventId,
                        cliente.Nome,
                        agendamento.DataHora,
                        60,
                        tipoAgendamento?.Nome,
                        tipoAgendamento?.Descricao,
                        new List<string> { cliente.Email },
                        tipoAgendamento?.Local,
                        tipoAgendamento?.CriarGoogleMeet ?? false,
                        tipoAgendamento?.CorCalendario ?? 6
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar Google Calendar");
                }
            }

            TempData["Mensagem"] = "✅ Agendamento atualizado com sucesso!";
            return RedirectToAction("MeusAgendamentos", new { token = token });
        }

        #endregion

        #region Ações para CLIENTES

        /// <summary>
        /// CLIENTE: Lista os agendamentos do cliente (via token ou autenticado)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MeusAgendamentos(string? token)
        {
            Cliente? cliente = null;
            
            // Se tiver token, validar via token
            if (!string.IsNullOrEmpty(token))
            {
                cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.MagicToken == token);

                if (cliente == null || !cliente.TokenValido())
                {
                    ViewBag.ClienteNome = cliente?.Nome;
                    ViewBag.ClienteId = cliente?.Id;
                    ViewBag.TokenExpiracao = cliente?.TokenExpiracao;
                    return View("../Auth/TokenExpirado");
                }
            }
            else
            {
                // Verificar autenticação
                var userType = GetUserType();
                if (userType != "Cliente")
                {
                    return RedirectToAction("Login", "Auth");
                }

                var clienteId = GetUsuarioId();
                if (clienteId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }
                
                cliente = await _context.Clientes.FindAsync(clienteId.Value);
                
                if (cliente == null)
                {
                    return RedirectToAction("Login", "Auth");
                }
            }

            // Buscar agendamentos futuros e não cancelados
            var agendamentos = await _context.Agendamentos
                .Include(a => a.TipoAgendamento)
                .Include(a => a.Funcionario)
                .Where(a => a.ClienteId == cliente.Id)
                .Where(a => a.DataHora >= DateTime.UtcNow.AddDays(-1)) // Incluir agendamentos de hoje
                .OrderBy(a => a.DataHora)
                .ToListAsync();

            ViewBag.Cliente = cliente;
            ViewBag.Token = token;

            return View(agendamentos);
        }

        /// <summary>
        /// CLIENTE: Exibe formulário para criar novo agendamento (via token ou autenticado)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Create(string? token)
        {
            Cliente? cliente = null;
            
            // Se tiver token, validar via token
            if (!string.IsNullOrEmpty(token))
            {
                cliente = await _context.Clientes
                    .Include(c => c.Funcionario)
                    .Include(c => c.FuncionarioResponsavel)
                    .FirstOrDefaultAsync(c => c.MagicToken == token);

                if (cliente == null || !cliente.TokenValido())
                {
                    ViewBag.ClienteNome = cliente?.Nome;
                    ViewBag.ClienteId = cliente?.Id;
                    ViewBag.TokenExpiracao = cliente?.TokenExpiracao;
                    return View("../Auth/TokenExpirado");
                }
            }
            else
            {
                // Verificar autenticação
                var userType = GetUserType();
                if (userType != "Cliente")
                {
                    return RedirectToAction("Login", "Auth");
                }

                var clienteId = GetUsuarioId();
                if (clienteId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Buscar informações do cliente
                cliente = await _context.Clientes
                    .Include(c => c.Funcionario)
                    .Include(c => c.FuncionarioResponsavel)
                    .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

                if (cliente == null)
                {
                    return NotFound();
                }
            }

            // Buscar documentos solicitados genéricos (sem tipo específico)
            // Os documentos específicos por tipo serão carregados via JS
            var documentos = await _context.DocumentosSolicitados
                .Where(d => d.Ativo && d.TipoAgendamentoId == null)
                .OrderByDescending(d => d.Obrigatorio)
                .ThenBy(d => d.Nome)
                .Select(d => new DocumentoUploadViewModel
                {
                    DocumentoSolicitadoId = d.Id,
                    Nome = d.Nome,
                    Descricao = d.Descricao,
                    Obrigatorio = d.Obrigatorio
                })
                .ToListAsync();

            // PRÉ-DEFINIR funcionário responsável (cliente não escolhe)
            var funcionarioId = cliente.FuncionarioResponsavelId ?? cliente.FuncionarioId;
            var funcionario = cliente.FuncionarioResponsavel ?? cliente.Funcionario;
            
            ViewBag.FuncionarioResponsavel = funcionario;
            ViewBag.FuncionarioId = funcionarioId;
            ViewBag.Token = token;

            // Buscar lista de funcionários para dropdown (apenas o responsável)
            ViewBag.Funcionarios = await _context.Funcionarios
                .Where(f => f.Id == funcionarioId)
                .ToListAsync();

            // Buscar tipos de agendamento ativos
            ViewBag.TiposAgendamento = await _context.TiposAgendamento
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();

            // Passar nome do cliente para o layout
            ViewBag.ClienteNome = cliente.Nome;
            ViewBag.Cliente = cliente;

            var viewModel = new AgendamentoCreateViewModel
            {
                FuncionarioId = funcionarioId,
                FuncionarioNome = funcionario?.Nome ?? "Não atribuído",
                Documentos = documentos
            };

            return View(viewModel);
        }

        /// <summary>
        /// CLIENTE: Processa a criação de um novo agendamento (via token ou autenticado)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AgendamentoCreateViewModel model, string? ParticipantesJson, string? token)
        {
            Cliente? cliente = null;
            
            // Se tiver token, validar via token
            if (!string.IsNullOrEmpty(token))
            {
                cliente = await _context.Clientes
                    .Include(c => c.Funcionario)
                    .Include(c => c.FuncionarioResponsavel)
                    .FirstOrDefaultAsync(c => c.MagicToken == token);

                if (cliente == null || !cliente.TokenValido())
                {
                    return View("../Auth/TokenExpirado");
                }
            }
            else
            {
                // Verificar autenticação
                var userType = GetUserType();
                if (userType != "Cliente")
                {
                    return RedirectToAction("Login", "Auth");
                }

                var clienteId = GetUsuarioId();
                if (clienteId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Buscar cliente
                cliente = await _context.Clientes
                    .Include(c => c.Funcionario)
                    .Include(c => c.FuncionarioResponsavel)
                    .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

                if (cliente == null)
                {
                    return NotFound();
                }
            }

            // ✅ CORREÇÃO: Guardar referência aos documentos ANTES de recarregar
            var documentosEnviados = model.Documentos;

            // Buscar o tipo de agendamento selecionado para validar documentos corretos
            var tipoSelecionado = await _context.TiposAgendamento
                .Include(t => t.DocumentosSolicitados)
                .FirstOrDefaultAsync(t => t.Id == model.TipoAgendamentoId);

            if (tipoSelecionado == null)
            {
                ModelState.AddModelError("TipoAgendamentoId", "Tipo de agendamento inválido");
                // Recarregar dados para view
                ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.Token = token;
                model.FuncionarioNome = cliente.FuncionarioResponsavel?.Nome ?? cliente.Funcionario?.Nome ?? "Não atribuído";
                return View(model);
            }

            // Recarregar documentos específicos do tipo + documentos genéricos
            var documentosNoBanco = await _context.DocumentosSolicitados
                .Where(d => d.Ativo && (d.TipoAgendamentoId == model.TipoAgendamentoId || d.TipoAgendamentoId == null))
                .OrderByDescending(d => d.Obrigatorio)
                .ThenBy(d => d.Nome)
                .ToListAsync();

            // ✅ CORREÇÃO: Reconstruir a lista MANTENDO os arquivos que foram enviados
            model.Documentos = documentosNoBanco.Select(d =>
            {
                // Procurar se este documento foi enviado
                var docEnviado = documentosEnviados?.FirstOrDefault(de => de.DocumentoSolicitadoId == d.Id);

                return new DocumentoUploadViewModel
                {
                    DocumentoSolicitadoId = d.Id,
                    Nome = d.Nome,
                    Descricao = d.Descricao,
                    Obrigatorio = d.Obrigatorio,
                    Arquivo = docEnviado?.Arquivo  // ✅ MANTÉM o arquivo enviado!
                };
            }).ToList();

            model.FuncionarioNome = cliente.FuncionarioResponsavel?.Nome ?? cliente.Funcionario?.Nome ?? "Não atribuído";

            if (!ModelState.IsValid)
            {
                // Recarregar dados para view
                ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.Token = token;
                return View(model);
            }

            // Validar data e hora do agendamento
            var validacao = ValidarDataHoraAgendamento(model.DataHora);
            if (!validacao.IsValid)
            {
                ModelState.AddModelError("DataHora", validacao.ErrorMessage);
                // Recarregar dados para view
                ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                ViewBag.ClienteNome = cliente.Nome;
                return View(model);
            }

            // Validar que todos os documentos obrigatórios foram enviados
            var documentosObrigatorios = documentosNoBanco.Where(d => d.Obrigatorio).ToList();

            _logger.LogInformation($"Validando {documentosObrigatorios.Count} documentos obrigatórios para tipo '{tipoSelecionado.Nome}'");

            foreach (var docObrigatorio in documentosObrigatorios)
            {
                // Procurar o documento correspondente no model
                var documentoEnviado = model.Documentos.FirstOrDefault(d => d.DocumentoSolicitadoId == docObrigatorio.Id);

                // Verificar se o arquivo foi enviado
                if (documentoEnviado?.Arquivo == null || documentoEnviado.Arquivo.Length == 0)
                {
                    _logger.LogWarning($"Documento obrigatório '{docObrigatorio.Nome}' (ID: {docObrigatorio.Id}) não foi enviado");
                    ModelState.AddModelError("", $"O documento '{docObrigatorio.Nome}' é obrigatório");
                }
                else
                {
                    _logger.LogInformation($"✓ Documento '{docObrigatorio.Nome}' OK: {documentoEnviado.Arquivo.FileName}");
                }
            }

            if (!ModelState.IsValid)
            {
                // Recarregar dados para view
                ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.Token = token;
                return View(model);
            }

            // Usar funcionário responsável ou fallback para funcionário principal
            var funcionario = cliente.FuncionarioResponsavel ?? cliente.Funcionario;
            var funcionarioId = cliente.FuncionarioResponsavelId ?? cliente.FuncionarioId;

            // Verificar disponibilidade no Google Calendar
            var disponivel = await _calendarService.VerificarDisponibilidadeAsync(
                funcionario?.GoogleCalendarEmail ?? "",
                model.DataHora
            );

            if (!disponivel)
            {
                ModelState.AddModelError("DataHora", "Este horário não está disponível. Por favor, escolha outro.");
                // Recarregar dados para view
                ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.Token = token;
                return View(model);
            }

            // Criar o agendamento
            var agendamento = new Agendamento
            {
                ClienteId = cliente.Id,
                FuncionarioId = funcionarioId,
                TipoAgendamentoId = model.TipoAgendamentoId,
                DataHora = model.DataHora,
                Status = "Pendente",
                Observacoes = model.Observacoes,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✓ Agendamento {agendamento.Id} criado com sucesso para {model.DataHora:yyyy-MM-dd HH:mm}");

            // ===== PROCESSAR PARTICIPANTES ADICIONAIS =====
            List<string> emailsParticipantes = new();

            if (!string.IsNullOrEmpty(ParticipantesJson))
            {
                try
                {
                    var participantesDeserialized = JsonSerializer.Deserialize<List<string>>(ParticipantesJson);
                    
                    if (participantesDeserialized != null)
                    {
                        // Validate each email before adding
                        var emailRegex = new System.Text.RegularExpressions.Regex(
                            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        emailsParticipantes = participantesDeserialized
                            .Where(email => !string.IsNullOrWhiteSpace(email) && emailRegex.IsMatch(email))
                            .Select(email => email.Trim())
                            .Distinct()
                            .ToList();
                        
                        _logger.LogInformation($"📧 Processando {emailsParticipantes.Count} participantes válidos");
                        
                        foreach (var email in emailsParticipantes)
                        {
                            var participante = new AgendamentoParticipante
                            {
                                AgendamentoId = agendamento.Id,
                                Email = email,
                                DataCriacao = DateTime.UtcNow
                            };
                            
                            _context.AgendamentoParticipantes.Add(participante);
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"✅ {emailsParticipantes.Count} participantes salvos no banco");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "❌ Erro ao processar JSON de participantes - formato inválido");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao processar participantes");
                }
            }

            // ===== INTEGRAÇÃO COM GOOGLE CALENDAR COM LOGS DETALHADOS =====
            var funcionarioEmail = cliente.Funcionario?.GoogleCalendarEmail;

            _logger.LogInformation($"");
            _logger.LogInformation($"📅 ========================================");
            _logger.LogInformation($"📅 INICIANDO INTEGRAÇÃO GOOGLE CALENDAR");
            _logger.LogInformation($"📅 ========================================");
            _logger.LogInformation($"   Cliente: {cliente.Nome}");
            _logger.LogInformation($"   Funcionário: {cliente.Funcionario?.Nome ?? "Não atribuído"}");
            _logger.LogInformation($"   Funcionário ID: {cliente.FuncionarioId}");
            _logger.LogInformation($"   Email do Funcionário: '{funcionarioEmail ?? "VAZIO!!!"}'");
            _logger.LogInformation($"   Data/Hora: {model.DataHora:yyyy-MM-dd HH:mm}");
            _logger.LogInformation($"");

            if (string.IsNullOrEmpty(funcionarioEmail))
            {
                _logger.LogWarning($"⚠️ ========================================");
                _logger.LogWarning($"⚠️ ATENÇÃO: EMAIL NÃO CONFIGURADO!");
                _logger.LogWarning($"⚠️ ========================================");
                _logger.LogWarning($"⚠️ Funcionário: '{cliente.Funcionario?.Nome ?? "desconhecido"}' (ID: {cliente.FuncionarioId})");
                _logger.LogWarning($"⚠️ NÃO possui email do Google Calendar configurado!");
                _logger.LogWarning($"⚠️ ");
                _logger.LogWarning($"⚠️ O agendamento foi SALVO no banco de dados,");
                _logger.LogWarning($"⚠️ mas o evento NÃO será criado no Google Calendar!");
                _logger.LogWarning($"⚠️ ");
                _logger.LogWarning($"⚠️ SOLUÇÃO:");
                _logger.LogWarning($"⚠️ 1. Faça login como Admin");
                _logger.LogWarning($"⚠️ 2. Vá em: Funcionários > Editar");
                _logger.LogWarning($"⚠️ 3. Preencha o campo 'Google Calendar Email'");
                _logger.LogWarning($"⚠️ 4. Use um email do Google Workspace");
                _logger.LogWarning($"⚠️ ========================================");
                _logger.LogInformation($"");
            }
            else
            {
                _logger.LogInformation($"✓ Email válido encontrado!");
                _logger.LogInformation($"✓ Chamando GoogleCalendarService.CriarEventoAsync...");
                _logger.LogInformation($"");

                // Buscar tipo de agendamento para obter configurações
                var tipoAgendamento = await _context.TiposAgendamento.FindAsync(model.TipoAgendamentoId);

                // Criar lista de TODOS os emails (cliente + participantes)
                var todosEmails = new List<string>();

                if (!string.IsNullOrEmpty(cliente?.Email))
                    todosEmails.Add(cliente.Email);

                todosEmails.AddRange(emailsParticipantes);

                _logger.LogInformation($"📧 Enviando convites para {todosEmails.Count} pessoa(s)");

                const int duracaoPadraoMinutos = 60; // Duração padrão de agendamentos

                var (eventId, conferenciaUrl) = await _calendarService.CriarEventoAsync(
                    funcionarioEmail,
                    cliente?.Nome ?? "Cliente",
                    model.DataHora,
                    duracaoPadraoMinutos,
                    tipoAgendamento?.Nome,
                    tipoAgendamento?.Descricao,
                    todosEmails,
                    tipoAgendamento?.Local,
                    tipoAgendamento?.CriarGoogleMeet ?? false,
                    tipoAgendamento?.CorCalendario ?? 6,
                    tipoAgendamento?.BloqueiaHorario ?? true
                );

                if (eventId != null)
                {
                    agendamento.GoogleCalendarEventId = eventId;
                    agendamento.ConferenciaUrl = conferenciaUrl;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ ========================================");
                    _logger.LogInformation($"✅ SUCESSO! EVENTO CRIADO NO GOOGLE CALENDAR");
                    _logger.LogInformation($"✅ ========================================");
                    _logger.LogInformation($"✅ Event ID: {eventId}");
                    _logger.LogInformation($"✅ Email: {funcionarioEmail}");
                    _logger.LogInformation($"✅ Cliente: {cliente?.Nome ?? "Cliente"}");
                    _logger.LogInformation($"✅ Data/Hora: {model.DataHora:yyyy-MM-dd HH:mm}");
                    if (!string.IsNullOrEmpty(conferenciaUrl))
                    {
                        _logger.LogInformation($"✅ Google Meet: {conferenciaUrl}");
                    }
                    _logger.LogInformation($"✅ ");
                    _logger.LogInformation($"✅ O evento agora está visível em:");
                    _logger.LogInformation($"✅ https://calendar.google.com");
                    _logger.LogInformation($"✅ ========================================");
                    _logger.LogInformation($"");
                }
                else
                {
                    _logger.LogError($"");
                    _logger.LogError($"❌ ========================================");
                    _logger.LogError($"❌ ERRO: FALHA AO CRIAR EVENTO!");
                    _logger.LogError($"❌ ========================================");
                    _logger.LogError($"❌ O GoogleCalendarService retornou NULL");
                    _logger.LogError($"❌ ");
                    _logger.LogError($"❌ Email usado: {funcionarioEmail}");
                    _logger.LogError($"❌ Cliente: {cliente?.Nome ?? "Cliente"}");
                    _logger.LogError($"❌ Data/Hora: {model.DataHora:yyyy-MM-dd HH:mm}");
                    _logger.LogError($"❌ ");
                    _logger.LogError($"❌ POSSÍVEIS CAUSAS:");
                    _logger.LogError($"❌ 1. Email não é do Google Workspace");
                    _logger.LogError($"❌ 2. Credenciais no appsettings.json incorretas");
                    _logger.LogError($"❌ 3. ClientId ou ClientSecret inválidos");
                    _logger.LogError($"❌ 4. RedirectUri não corresponde ao Google Cloud");
                    _logger.LogError($"❌ 5. Usuário não autorizou o acesso");
                    _logger.LogError($"❌ 6. API do Google Calendar não ativada");
                    _logger.LogError($"❌ ");
                    _logger.LogError($"❌ VERIFICAR:");
                    _logger.LogError($"❌ - appsettings.json tem ClientId e ClientSecret?");
                    _logger.LogError($"❌ - Google Cloud Console > Credentials está OK?");
                    _logger.LogError($"❌ - Google Cloud Console > OAuth consent screen configurado?");
                    _logger.LogError($"❌ ========================================");
                    _logger.LogError($"");
                }
            }

            // ✅ NOVO: Fazer upload dos documentos anexados (COM COMPRESSÃO)
            int documentosSalvos = 0;
            long totalOriginal = 0;
            long totalComprimido = 0;

            foreach (var documento in model.Documentos)
            {
                if (documento.Arquivo != null && documento.Arquivo.Length > 0)
                {
                    _logger.LogInformation($"Processando arquivo: {documento.Arquivo.FileName}");

                    // ✅ Processar e comprimir arquivo
                    var uploadResult = await _fileUploadService.ProcessarArquivoAsync(documento.Arquivo);

                    if (uploadResult.Success && uploadResult.ConteudoComprimido != null)
                    {
                        // ✅ Salvar no banco de dados (comprimido)
                        var documentoAnexado = new DocumentoAnexado
                        {
                            AgendamentoId = agendamento.Id,
                            DocumentoSolicitadoId = documento.DocumentoSolicitadoId,
                            NomeArquivo = documento.Arquivo.FileName,
                            ConteudoComprimido = uploadResult.ConteudoComprimido,  // ✅ Bytes comprimidos
                            TamanhoOriginalBytes = uploadResult.TamanhoOriginal,
                            TamanhoComprimidoBytes = uploadResult.TamanhoComprimido,
                            DataUpload = DateTime.UtcNow
                        };

                        _context.DocumentosAnexados.Add(documentoAnexado);
                        documentosSalvos++;
                        totalOriginal += uploadResult.TamanhoOriginal;
                        totalComprimido += uploadResult.TamanhoComprimido;

                        _logger.LogInformation(
                            $"✓ '{documento.Arquivo.FileName}': " +
                            $"{uploadResult.TamanhoOriginal:N0} → {uploadResult.TamanhoComprimido:N0} bytes"
                        );
                    }
                    else
                    {
                        _logger.LogError($"❌ Erro ao processar '{documento.Arquivo.FileName}': {uploadResult.ErrorMessage}");
                    }
                }
            }

            await _context.SaveChangesAsync();

            var reducao = totalOriginal > 0 ? (1 - ((double)totalComprimido / totalOriginal)) * 100 : 0;
            _logger.LogInformation(
                $"🎉 {documentosSalvos} documentos salvos | " +
                $"Total: {totalOriginal:N0} → {totalComprimido:N0} bytes ({reducao:F1}% de redução)"
            );

            TempData["SuccessMessage"] = "Agendamento criado com sucesso!";
            
            if (!string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(MeusAgendamentos), new { token = token });
            }
            
            return RedirectToAction(nameof(MeusAgendamentos));
        }

        /// <summary>
        /// CLIENTE: Cancela um agendamento próprio (apenas se faltar mais de 24h)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMeu(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Cliente")
            {
                return RedirectToAction("Login", "Auth");
            }

            var clienteId = GetUsuarioId();
            if (clienteId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Buscar agendamento
            var agendamento = await _context.Agendamentos
                .Include(a => a.Funcionario)
                .FirstOrDefaultAsync(a => a.Id == id && a.ClienteId == clienteId.Value);

            if (agendamento == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado.";
                return RedirectToAction(nameof(MeusAgendamentos));
            }

            // Verificar se o agendamento já foi cancelado
            if (agendamento.Status == "Cancelado")
            {
                TempData["ErrorMessage"] = "Este agendamento já está cancelado.";
                return RedirectToAction(nameof(MeusAgendamentos));
            }

            // Verificar se faltam mais de 24 horas
            var horasRestantes = (agendamento.DataHora - DateTime.UtcNow).TotalHours;
            if (horasRestantes < 24)
            {
                TempData["ErrorMessage"] = "Você só pode cancelar agendamentos com mais de 24 horas de antecedência.";
                return RedirectToAction(nameof(MeusAgendamentos));
            }

            // Cancelar o agendamento
            agendamento.Status = "Cancelado";
            agendamento.DataAtualizacao = DateTime.UtcNow;

            // Deletar evento do Google Calendar
            if (!string.IsNullOrEmpty(agendamento.GoogleCalendarEventId))
            {
                await _calendarService.DeletarEventoAsync(
                    agendamento.Funcionario?.GoogleCalendarEmail ?? "",
                    agendamento.GoogleCalendarEventId
                );
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Agendamento cancelado com sucesso.";
            return RedirectToAction(nameof(MeusAgendamentos));
        }

        #endregion

        #region Ações para FUNCIONÁRIOS e ADMINISTRADORES

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Lista agendamentos
        /// Funcionário vê apenas seus agendamentos
        /// Admin vê todos os agendamentos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(AgendamentoIndexViewModel model)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Construir query base
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.TipoAgendamento)
                .Include(a => a.DocumentosAnexados)
                .AsQueryable();

            // Se não for admin, filtrar apenas agendamentos do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            // Aplicar filtros
            if (!string.IsNullOrEmpty(model.FiltroStatus))
            {
                query = query.Where(a => a.Status == model.FiltroStatus);
            }

            if (model.FiltroDataInicio.HasValue)
            {
                query = query.Where(a => a.DataHora.Date >= model.FiltroDataInicio.Value.Date);
            }

            if (model.FiltroDataFim.HasValue)
            {
                query = query.Where(a => a.DataHora.Date <= model.FiltroDataFim.Value.Date);
            }

            // Filtro por funcionário (apenas para admin)
            if (isAdmin && model.FiltroFuncionarioId.HasValue)
            {
                query = query.Where(a => a.FuncionarioId == model.FiltroFuncionarioId.Value);
            }

            // Buscar agendamentos
            var agendamentos = await query
                .OrderBy(a => a.DataHora)
                .Select(a => new AgendamentoListItem
                {
                    Id = a.Id,
                    DataHora = a.DataHora,
                    Status = a.Status,
                    ClienteNome = a.Cliente!.Nome,
                    ClienteCPF = a.Cliente.CPF,
                    ClienteEmail = a.Cliente.Email,
                    FuncionarioNome = a.Funcionario!.Nome,
                    TipoAgendamentoNome = a.TipoAgendamento != null ? a.TipoAgendamento.Nome : null,
                    TotalDocumentos = a.DocumentosAnexados.Count,
                    DataCriacao = a.DataCriacao
                })
                .ToListAsync();

            model.Agendamentos = agendamentos;

            // Se for admin, carregar lista de funcionários para o filtro
            if (isAdmin)
            {
                model.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .Select(f => new FuncionarioSelectItem
                    {
                        Id = f.Id,
                        Nome = f.Nome
                    })
                    .ToListAsync();
            }

            return View(model);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Exibe formulário para criar novo agendamento para um cliente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateAgendamento()
        {
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Carregar tipos ativos
            ViewBag.TiposAgendamento = await _context.TiposAgendamento
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();

            // Carregar clientes
            var query = _context.Clientes.Where(c => c.Ativo).AsQueryable();
            
            if (!isAdmin)
            {
                query = query.Where(c => c.FuncionarioId == funcionarioId.Value);
            }

            ViewBag.Clientes = await query.OrderBy(c => c.Nome).ToListAsync();

            // Buscar lista de funcionários para dropdown
            if (isAdmin)
            {
                // Admin vê TODOS os funcionários ativos
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
            }
            else
            {
                // Funcionário comum vê SÓ ele mesmo
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Id == funcionarioId.Value)
                    .ToListAsync();
            }

            var model = new AgendamentoCreateViewModel();
            return View(model);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Processa criação de novo agendamento para um cliente
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgendamento(AgendamentoCreateViewModel model, IFormCollection form, string? ParticipantesJson)
        {
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Validação básica
            if (model.ClienteId == 0)
            {
                ModelState.AddModelError("ClienteId", "Selecione um cliente");
            }

            if (model.TipoAgendamentoId == 0)
            {
                ModelState.AddModelError("TipoAgendamentoId", "Selecione o tipo de agendamento");
            }

            if (ModelState.IsValid)
            {
                // ✅ VALIDAR DOCUMENTOS OBRIGATÓRIOS
                var docsObrigatorios = await _context.DocumentosSolicitados
                    .Where(d => d.TipoAgendamentoId == model.TipoAgendamentoId && d.Obrigatorio && d.Ativo)
                    .ToListAsync();

                // Verificar se todos os obrigatórios foram enviados
                foreach (var docObrigatorio in docsObrigatorios)
                {
                    var arquivoKey = $"documento_{docObrigatorio.Id}";
                    var arquivo = form.Files.FirstOrDefault(f => f.Name == arquivoKey);
                    if (arquivo == null || arquivo.Length == 0)
                    {
                        ModelState.AddModelError("", $"O documento '{docObrigatorio.Nome}' é obrigatório e não foi anexado.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    await CarregarViewBags(isAdmin, funcionarioId.Value);
                    return View(model);
                }

                // Determinar responsável
                int responsavelId;
                if (isAdmin && model.FuncionarioId > 0)
                {
                    responsavelId = model.FuncionarioId; // Admin escolheu
                }
                else
                {
                    responsavelId = funcionarioId.Value; // Funcionário logado
                }

                // Validar data/hora
                var validacao = ValidarDataHoraAgendamento(model.DataHora);
                if (!validacao.IsValid)
                {
                    ModelState.AddModelError("DataHora", validacao.ErrorMessage);
                    await CarregarViewBags(isAdmin, funcionarioId.Value);
                    return View(model);
                }

                var agendamento = new Agendamento
                {
                    ClienteId = model.ClienteId,
                    FuncionarioId = responsavelId,
                    TipoAgendamentoId = model.TipoAgendamentoId,
                    DataHora = model.DataHora,
                    Status = "Pendente",
                    Observacoes = model.Observacoes,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                };

                _context.Add(agendamento);
                await _context.SaveChangesAsync();

                // ===== PROCESSAR PARTICIPANTES ADICIONAIS =====
                List<string> emailsParticipantes = new();

                if (!string.IsNullOrEmpty(ParticipantesJson))
                {
                    try
                    {
                        var participantesDeserialized = JsonSerializer.Deserialize<List<string>>(ParticipantesJson);
                        
                        if (participantesDeserialized != null)
                        {
                            // Validate each email before adding
                            var emailRegex = new System.Text.RegularExpressions.Regex(
                                @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            emailsParticipantes = participantesDeserialized
                                .Where(email => !string.IsNullOrWhiteSpace(email) && emailRegex.IsMatch(email))
                                .Select(email => email.Trim())
                                .Distinct()
                                .ToList();
                            
                            _logger.LogInformation($"📧 Processando {emailsParticipantes.Count} participantes válidos");
                            
                            foreach (var email in emailsParticipantes)
                            {
                                var participante = new AgendamentoParticipante
                                {
                                    AgendamentoId = agendamento.Id,
                                    Email = email,
                                    DataCriacao = DateTime.UtcNow
                                };
                                
                                _context.AgendamentoParticipantes.Add(participante);
                            }
                            
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"✅ {emailsParticipantes.Count} participantes salvos no banco");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao processar JSON de participantes - formato inválido");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao processar participantes");
                    }
                }

                // ✅ PROCESSAR UPLOADS INDIVIDUAIS
                await ProcessarUploadIndividual(form.Files, agendamento.Id);

                // ✅ INTEGRAÇÃO GOOGLE CALENDAR
                try
                {
                    // Buscar cliente, funcionário e tipo de agendamento
                    var cliente = await _context.Clientes.FindAsync(agendamento.ClienteId);
                    var funcionario = await _context.Funcionarios.FindAsync(agendamento.FuncionarioId);
                    var tipoAgendamento = await _context.TiposAgendamento.FindAsync(agendamento.TipoAgendamentoId);

                    if (funcionario != null && !string.IsNullOrEmpty(funcionario.GoogleCalendarEmail))
                    {
                        _logger.LogInformation($"📅 Iniciando criação de evento no Google Calendar para funcionário {funcionario.GoogleCalendarEmail}");
                        
                        var clienteNome = cliente?.Nome ?? "Cliente";
                        var local = tipoAgendamento?.Local;
                        var criarGoogleMeet = tipoAgendamento?.CriarGoogleMeet ?? false;
                        var corCalendario = tipoAgendamento?.CorCalendario ?? 6;
                        var bloqueiaHorario = tipoAgendamento?.BloqueiaHorario ?? true;

                        // Criar lista de TODOS os emails (cliente + participantes)
                        var todosEmails = new List<string>();

                        if (!string.IsNullOrEmpty(cliente?.Email))
                            todosEmails.Add(cliente.Email);

                        todosEmails.AddRange(emailsParticipantes);

                        _logger.LogInformation($"📧 Enviando convites para {todosEmails.Count} pessoa(s)");

                        const int duracaoPadraoMinutos = 60; // Duração padrão de agendamentos

                        var (googleEventId, conferenciaUrl) = await _calendarService.CriarEventoAsync(
                            funcionario.GoogleCalendarEmail,
                            clienteNome,
                            agendamento.DataHora,
                            duracaoPadraoMinutos,
                            tipoAgendamento?.Nome,
                            tipoAgendamento?.Descricao,
                            todosEmails,
                            local,
                            criarGoogleMeet,
                            corCalendario,
                            bloqueiaHorario
                        );

                        if (!string.IsNullOrEmpty(googleEventId))
                        {
                            agendamento.GoogleCalendarEventId = googleEventId;
                            agendamento.ConferenciaUrl = conferenciaUrl;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"✅ Evento criado no Google Calendar. EventId: {googleEventId}");
                            
                            if (!string.IsNullOrEmpty(conferenciaUrl))
                            {
                                _logger.LogInformation($"🎥 Google Meet: {conferenciaUrl}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Não foi possível criar evento no Google Calendar. Funcionário pode precisar autorizar OAuth.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Funcionário não possui Google Calendar configurado, pulando integração.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao criar evento no Google Calendar, mas agendamento foi salvo.");
                    // Não falhar o agendamento por erro no Google Calendar
                }

                _logger.LogInformation($"Agendamento {agendamento.Id} criado por {User.Identity?.Name}");

                TempData["SuccessMessage"] = "Agendamento criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            await CarregarViewBags(isAdmin, funcionarioId.Value);
            return View(model);
        }

        /// <summary>
        /// Helper para carregar ViewBags necessárias
        /// </summary>
        private async Task CarregarViewBags(bool isAdmin, int funcionarioId)
        {
            ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).OrderBy(t => t.Nome).ToListAsync();
            
            var queryClientes = _context.Clientes.Where(c => c.Ativo).AsQueryable();
            if (!isAdmin)
            {
                queryClientes = queryClientes.Where(c => c.FuncionarioId == funcionarioId);
            }
            ViewBag.Clientes = await queryClientes.OrderBy(c => c.Nome).ToListAsync();

            if (isAdmin)
            {
                ViewBag.Funcionarios = await _context.Funcionarios.Where(f => f.Ativo).OrderBy(f => f.Nome).ToListAsync();
            }
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Visualiza detalhes de um agendamento
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.DocumentosAnexados)
                    .ThenInclude(da => da.DocumentoSolicitado)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                return NotFound();
            }

            return View(agendamento);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Exibe formulário para editar agendamento
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.TipoAgendamento)
                .Include(a => a.Participantes)  // Incluir participantes
                .Include(a => a.DocumentosAnexados)
                    .ThenInclude(da => da.DocumentoSolicitado)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                return NotFound();
            }

            // Carregar lista de funcionários para dropdown
            if (isAdmin)
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Id == funcionarioId.Value)
                    .ToListAsync();
            }

            // Carregar tipos de agendamento
            ViewBag.TiposAgendamento = await _context.TiposAgendamento
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();

            var viewModel = new AgendamentoEditViewModel
            {
                Id = agendamento.Id,
                Status = agendamento.Status,
                Observacoes = agendamento.Observacoes,
                DataHora = agendamento.DataHora,
                DataHoraOriginal = agendamento.DataHora,
                GoogleCalendarEventId = agendamento.GoogleCalendarEventId,
                FuncionarioGoogleEmail = agendamento.Funcionario?.GoogleCalendarEmail,
                ClienteId = agendamento.ClienteId,
                ClienteNome = agendamento.Cliente?.Nome ?? "",
                ClienteEmail = agendamento.Cliente?.Email ?? "",
                ClienteTelefone = agendamento.Cliente?.Telefone ?? "",
                FuncionarioId = agendamento.FuncionarioId,
                FuncionarioNome = agendamento.Funcionario?.Nome ?? "",
                TipoAgendamentoId = agendamento.TipoAgendamentoId,
                Participantes = agendamento.Participantes?.ToList() ?? new List<AgendamentoParticipante>(),
                DocumentosAnexados = agendamento.DocumentosAnexados?.ToList() ?? new List<DocumentoAnexado>()
            };

            return View(viewModel);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Processa a edição de um agendamento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, 
            AgendamentoEditViewModel model,
            string? ParticipantesJson)  // Adicionar parâmetro
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.TipoAgendamento)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                return NotFound();
            }

            // Detectar mudança de status para Cancelado
            bool statusMudouParaCancelado = agendamento.Status != "Cancelado" && model.Status == "Cancelado";
            
            // Detectar mudança de data/hora ou funcionário
            bool dataHoraMudou = agendamento.DataHora != model.DataHora;
            bool funcionarioMudou = agendamento.FuncionarioId != model.FuncionarioId;

            // Atualizar campos
            agendamento.Status = model.Status;
            agendamento.Observacoes = model.Observacoes;
            agendamento.DataHora = model.DataHora;
            agendamento.FuncionarioId = model.FuncionarioId;
            if (model.TipoAgendamentoId.HasValue)
            {
                agendamento.TipoAgendamentoId = model.TipoAgendamentoId.Value;
            }
            agendamento.DataAtualizacao = DateTime.UtcNow;

            // ===== ATUALIZAR PARTICIPANTES =====
            // Remover participantes antigos
            var participantesAntigos = await _context.AgendamentoParticipantes
                .Where(p => p.AgendamentoId == id)
                .ToListAsync();

            _context.AgendamentoParticipantes.RemoveRange(participantesAntigos);

            // Adicionar novos participantes
            List<string> emailsParticipantes = new();

            if (!string.IsNullOrEmpty(ParticipantesJson))
            {
                try
                {
                    emailsParticipantes = JsonSerializer.Deserialize<List<string>>(ParticipantesJson) 
                        ?? new List<string>();
                    
                    foreach (var email in emailsParticipantes)
                    {
                        var participante = new AgendamentoParticipante
                        {
                            AgendamentoId = id,
                            Email = email,
                            DataCriacao = DateTime.UtcNow
                        };
                        
                        _context.AgendamentoParticipantes.Add(participante);
                    }
                    
                    _logger.LogInformation($"✅ {emailsParticipantes.Count} participantes atualizados");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao processar participantes");
                }
            }

            await _context.SaveChangesAsync();

            // ✅ INTEGRAÇÃO GOOGLE CALENDAR
            try
            {
                var funcionario = await _context.Funcionarios.FindAsync(agendamento.FuncionarioId);
                var cliente = await _context.Clientes.FindAsync(agendamento.ClienteId);
                var tipoAgendamento = await _context.TiposAgendamento.FindAsync(agendamento.TipoAgendamentoId);
                var funcionarioEmail = funcionario?.GoogleCalendarEmail;
                var eventId = agendamento.GoogleCalendarEventId;

                if (!string.IsNullOrEmpty(funcionarioEmail) && !string.IsNullOrEmpty(eventId))
                {
                    // Se status mudou para Cancelado, deletar evento
                    if (statusMudouParaCancelado)
                    {
                        _logger.LogInformation($"🗑️ Deletando evento do Google Calendar: {eventId}");
                        var deletado = await _calendarService.DeletarEventoAsync(funcionarioEmail, eventId);
                        
                        if (deletado)
                        {
                            _logger.LogInformation($"✅ Evento deletado do Google Calendar");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Não foi possível deletar evento do Google Calendar");
                        }
                    }
                    // Se data/hora, funcionário ou participantes mudaram, atualizar evento
                    else if (dataHoraMudou || funcionarioMudou || !string.IsNullOrEmpty(ParticipantesJson))
                    {
                        _logger.LogInformation($"📅 Atualizando evento no Google Calendar: {eventId}");
                        
                        // Montar lista de emails (cliente + participantes)
                        var todosEmails = new List<string>();
                        
                        if (!string.IsNullOrEmpty(cliente?.Email))
                            todosEmails.Add(cliente.Email);
                        
                        todosEmails.AddRange(emailsParticipantes);
                        
                        _logger.LogInformation($"📧 Atualizando evento no Google Calendar para {todosEmails.Count} pessoa(s)");
                        
                        // Atualizar evento existente com todos os parâmetros
                        var atualizado = await _calendarService.AtualizarEventoAsync(
                            funcionarioEmail,
                            eventId,
                            cliente?.Nome ?? "Cliente",
                            agendamento.DataHora,
                            60,
                            tipoAgendamento?.Nome,
                            tipoAgendamento?.Descricao,
                            todosEmails,
                            tipoAgendamento?.Local,
                            tipoAgendamento?.CriarGoogleMeet ?? false,
                            tipoAgendamento?.CorCalendario ?? 6
                        );
                        
                        if (atualizado)
                        {
                            _logger.LogInformation($"✅ Evento atualizado no Google Calendar");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Não foi possível atualizar evento no Google Calendar");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ Agendamento não possui Google Calendar configurado, pulando integração.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao integrar com Google Calendar, mas agendamento foi atualizado.");
                // Não falhar a atualização por erro no Google Calendar
            }

            TempData["SuccessMessage"] = "Agendamento atualizado com sucesso!";
            return RedirectToAction(nameof(Details), new { id = agendamento.Id });
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Cancela um agendamento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Funcionario)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Cancelar o agendamento
            agendamento.Status = "Cancelado";
            agendamento.DataAtualizacao = DateTime.UtcNow;

            // Deletar evento do Google Calendar
            if (!string.IsNullOrEmpty(agendamento.GoogleCalendarEventId))
            {
                await _calendarService.DeletarEventoAsync(
                    agendamento.Funcionario?.GoogleCalendarEmail ?? "",
                    agendamento.GoogleCalendarEventId
                );
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Agendamento cancelado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Exibe confirmação para deletar agendamento
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.TipoAgendamento)
                .Include(a => a.DocumentosAnexados)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                return NotFound();
            }

            return View(agendamento);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Confirma e executa deleção do agendamento
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar agendamento
            var query = _context.Agendamentos
                .Include(a => a.Funcionario)
                .Include(a => a.DocumentosAnexados)
                .AsQueryable();

            // Se não for admin, verificar se é do funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId.Value);
            }

            var agendamento = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                return NotFound();
            }

            // Deletar documentos anexados primeiro
            if (agendamento.DocumentosAnexados != null && agendamento.DocumentosAnexados.Any())
            {
                _context.DocumentosAnexados.RemoveRange(agendamento.DocumentosAnexados);
            }

            // Deletar evento do Google Calendar (se existir)
            if (!string.IsNullOrEmpty(agendamento.GoogleCalendarEventId))
            {
                await _calendarService.DeletarEventoAsync(
                    agendamento.Funcionario?.GoogleCalendarEmail ?? "",
                    agendamento.GoogleCalendarEventId
                );
            }

            // Deletar agendamento
            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Agendamento {id} deletado por {User.Identity?.Name}");

            TempData["SuccessMessage"] = "Agendamento deletado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Faz download de um documento anexado
        /// ✅ NOVO: Com descompressão automática
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadDocumento(int id)
        {
            // Verificar autenticação
            var userType = GetUserType();
            if (userType != "Funcionario")
            {
                return RedirectToAction("Login", "Auth");
            }

            var funcionarioId = GetUsuarioId();
            if (funcionarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isAdmin = IsAdmin();

            // Buscar documento
            var query = _context.DocumentosAnexados
                .Include(da => da.Agendamento)
                .AsQueryable();

            // Se não for admin, verificar se o documento pertence a um agendamento do funcionário
            if (!isAdmin)
            {
                query = query.Where(da => da.Agendamento!.FuncionarioId == funcionarioId.Value);
            }

            var documento = await query.FirstOrDefaultAsync(da => da.Id == id);

            if (documento == null)
            {
                return NotFound();
            }

            // ✅ Descomprimir arquivo
            byte[] conteudoDescomprimido;
            try
            {
                conteudoDescomprimido = _fileUploadService.DescomprimirArquivo(documento.ConteudoComprimido);

                _logger.LogInformation(
                    $"Download: '{documento.NomeArquivo}' " +
                    $"({documento.TamanhoComprimidoBytes:N0} → {conteudoDescomprimido.Length:N0} bytes)"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao descomprimir '{documento.NomeArquivo}'");
                TempData["ErrorMessage"] = "Erro ao processar o arquivo.";
                return RedirectToAction(nameof(Details), new { id = documento.AgendamentoId });
            }

            // Retornar arquivo para download
            var contentType = GetContentType(documento.NomeArquivo);
            return File(conteudoDescomprimido, contentType, documento.NomeArquivo);
        }

        /// <summary>
        /// Processa upload individual de documentos
        /// </summary>
        private async Task ProcessarUploadIndividual(IFormFileCollection arquivos, int agendamentoId)
        {
            int documentosSalvos = 0;
            long totalOriginal = 0;
            long totalComprimido = 0;

            foreach (var arquivo in arquivos)
            {
                // Extrair DocumentoSolicitadoId do nome do campo (documento_{id})
                if (!arquivo.Name.StartsWith("documento_"))
                    continue;

                var documentoIdStr = arquivo.Name.Replace("documento_", "");
                if (!int.TryParse(documentoIdStr, out int documentoSolicitadoId))
                    continue;

                if (arquivo.Length > 0)
                {
                    // Validar tamanho (10MB)
                    if (arquivo.Length > 10 * 1024 * 1024)
                    {
                        _logger.LogWarning($"Arquivo {arquivo.FileName} excede 10MB");
                        continue;
                    }

                    // Validar extensão
                    var extensao = Path.GetExtension(arquivo.FileName).ToLower();
                    if (extensao != ".pdf" && extensao != ".jpg" && extensao != ".jpeg" && extensao != ".png")
                    {
                        _logger.LogWarning($"Arquivo {arquivo.FileName} tem extensão inválida");
                        continue;
                    }

                    // Processar e comprimir arquivo
                    var uploadResult = await _fileUploadService.ProcessarArquivoAsync(arquivo);

                    if (uploadResult.Success && uploadResult.ConteudoComprimido != null)
                    {
                        // Salvar no banco de dados (comprimido)
                        var documentoAnexado = new DocumentoAnexado
                        {
                            AgendamentoId = agendamentoId,
                            DocumentoSolicitadoId = documentoSolicitadoId,
                            NomeArquivo = arquivo.FileName,
                            ConteudoComprimido = uploadResult.ConteudoComprimido,
                            TamanhoOriginalBytes = uploadResult.TamanhoOriginal,
                            TamanhoComprimidoBytes = uploadResult.TamanhoComprimido,
                            DataUpload = DateTime.UtcNow
                        };

                        _context.DocumentosAnexados.Add(documentoAnexado);
                        documentosSalvos++;
                        totalOriginal += uploadResult.TamanhoOriginal;
                        totalComprimido += uploadResult.TamanhoComprimido;

                        _logger.LogInformation(
                            $"✓ '{arquivo.FileName}': " +
                            $"{uploadResult.TamanhoOriginal:N0} → {uploadResult.TamanhoComprimido:N0} bytes"
                        );
                    }
                    else
                    {
                        _logger.LogError($"❌ Erro ao processar '{arquivo.FileName}': {uploadResult.ErrorMessage}");
                    }
                }
            }

            if (documentosSalvos > 0)
            {
                await _context.SaveChangesAsync();
                var reducao = totalOriginal > 0 ? (1 - ((double)totalComprimido / totalOriginal)) * 100 : 0;
                _logger.LogInformation(
                    $"🎉 {documentosSalvos} documentos salvos | " +
                    $"Total: {totalOriginal:N0} → {totalComprimido:N0} bytes ({reducao:F1}% de redução)"
                );
            }
        }

        /// <summary>
        /// Obtém o content type baseado na extensão do arquivo
        /// </summary>
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}