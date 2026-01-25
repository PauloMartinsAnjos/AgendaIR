using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgendaIR.Services;
using AgendaIR.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgendaIR.Controllers
{
    [Authorize]
    public class GoogleCalendarController : Controller
    {
        private readonly GoogleCalendarService _calendarService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleCalendarController> _logger;

        public GoogleCalendarController(
            GoogleCalendarService calendarService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<GoogleCalendarController> logger)
        {
            _calendarService = calendarService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Inicia fluxo de autorização OAuth do Google Calendar
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> IniciarAutorizacao()
        {
            var funcionarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(funcionarioIdClaim) || !int.TryParse(funcionarioIdClaim, out int funcionarioId))
            {
                TempData["Error"] = "Usuário não autenticado";
                return RedirectToAction("Login", "Auth");
            }

            var funcionario = await _context.Funcionarios.FindAsync(funcionarioId);

            if (funcionario == null)
            {
                TempData["Error"] = "Funcionário não encontrado";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(funcionario.GoogleCalendarEmail))
            {
                TempData["Error"] = "Configure seu email do Google Calendar nas configurações antes de autorizar";
                return RedirectToAction("Index", "Home");
            }

            // Gerar URL de autorização
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var redirectUri = _configuration["GoogleCalendar:RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            {
                TempData["Error"] = "Google Calendar não está configurado. Contate o administrador.";
                _logger.LogError("ClientId ou RedirectUri não configurados no appsettings.json");
                return RedirectToAction("Index", "Home");
            }

            var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(funcionario.GoogleCalendarEmail));

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                          $"client_id={Uri.EscapeDataString(clientId)}" +
                          $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                          $"&response_type=code" +
                          $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                          $"&state={state}" +
                          $"&access_type=offline" +
                          $"&prompt=consent";

            _logger.LogInformation($"🔐 Redirecionando {funcionario.Nome} para autorização OAuth");

            return Redirect(authUrl);
        }

        /// <summary>
        /// Callback do Google OAuth - recebe o código de autorização
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Callback(string? code, string? state, string? error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                TempData["Error"] = $"Erro na autorização: {error}";
                _logger.LogError($"Erro OAuth: {error}");
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                TempData["Error"] = "Autorização cancelada ou inválida";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var sucesso = await _calendarService.ProcessarCodigoOAuthAsync(code, state);

                if (sucesso)
                {
                    TempData["Success"] = "✅ Google Calendar autorizado com sucesso! Agora você pode receber agendamentos.";
                    _logger.LogInformation($"✅ OAuth autorizado com sucesso");
                }
                else
                {
                    TempData["Error"] = "Erro ao processar autorização. Tente novamente.";
                    _logger.LogError("Falha ao processar código OAuth");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erro ao processar autorização do Google Calendar";
                _logger.LogError(ex, "Erro ao processar callback OAuth");
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Remove autorização do Google Calendar (desconectar)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevogarAutorizacao()
        {
            var funcionarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(funcionarioIdClaim) || !int.TryParse(funcionarioIdClaim, out int funcionarioId))
            {
                TempData["Error"] = "Usuário não autenticado";
                return RedirectToAction("Login", "Auth");
            }

            var funcionario = await _context.Funcionarios.FindAsync(funcionarioId);

            if (funcionario != null)
            {
                funcionario.GoogleCalendarToken = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Autorização do Google Calendar removida";
                _logger.LogInformation($"Funcionário {funcionario.Nome} revogou autorização Google Calendar");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
