using AgendaIR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável por receber o callback do Google OAuth
    /// </summary>
    public class GoogleAuthController : Controller
    {
        private readonly GoogleCalendarService _calendarService;
        private readonly ILogger<GoogleAuthController> _logger;

        public GoogleAuthController(
            GoogleCalendarService calendarService,
            ILogger<GoogleAuthController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        /// <summary>
        /// Recebe o callback do Google OAuth após autorização
        /// URL configurada no Google Cloud Console
        /// </summary>
        [HttpGet]
        [Route("/auth/google/callback")]
        public async Task<IActionResult> GoogleCallback(string code, string state, string error)
        {
            try
            {
                // Verificar se houve erro na autorização
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError($"❌ Erro na autorização OAuth: {error}");

                    TempData["ErrorMessage"] = $"Erro ao autorizar Google Calendar: {error}";

                    // Redirecionar de volta
                    return RedirectToAction("Index", "Home");
                }

                // Verificar se recebeu o código
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("⚠️ Callback recebido sem código de autorização");

                    TempData["ErrorMessage"] = "Não foi possível completar a autorização.";

                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation($"✅ Callback OAuth recebido!");
                _logger.LogInformation($"   Code: {code.Substring(0, Math.Min(20, code.Length))}...");
                _logger.LogInformation($"   State: {state}");

                // Processar código de autorização
                var sucesso = await _calendarService.ProcessarCodigoOAuthAsync(code, state);

                if (sucesso)
                {
                    _logger.LogInformation($"🎉 Autorização concluída com sucesso!");

                    TempData["SuccessMessage"] = "Google Calendar autorizado com sucesso! Agora os agendamentos serão sincronizados.";

                    // Fechar janela popup (se foi aberta em popup)
                    ViewBag.CloseWindow = true;

                    return View("AuthSuccess");
                }
                else
                {
                    _logger.LogError($"❌ Falha ao processar código OAuth");

                    TempData["ErrorMessage"] = "Erro ao processar autorização do Google Calendar.";

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar callback OAuth");

                TempData["ErrorMessage"] = "Erro inesperado ao processar autorização.";

                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Iniciar manualmente o fluxo de autorização OAuth
        /// Útil para quando o funcionário precisa autorizar pela primeira vez
        /// </summary>
        [HttpGet]
        [Route("/auth/google/authorize")]
        public IActionResult Authorize(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "Email não fornecido.";
                    return RedirectToAction("Index", "Home");
                }

                var clientId = HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["GoogleCalendar:ClientId"];

                var redirectUri = HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["GoogleCalendar:RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                {
                    TempData["ErrorMessage"] = "Google Calendar não está configurado.";
                    return RedirectToAction("Index", "Home");
                }

                var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(email));

                var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                              $"client_id={Uri.EscapeDataString(clientId)}" +
                              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                              $"&response_type=code" +
                              $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                              $"&state={state}" +
                              $"&access_type=offline" +
                              $"&prompt=consent" +
                              $"&login_hint={Uri.EscapeDataString(email)}";

                _logger.LogInformation($"🔐 Redirecionando para autorização OAuth para {email}");

                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar autorização OAuth");

                TempData["ErrorMessage"] = "Erro ao iniciar autorização.";

                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Status da autorização OAuth
        /// </summary>
        [HttpGet]
        [Route("/auth/google/status")]
        public async Task<IActionResult> Status(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { authorized = false, message = "Email não fornecido" });
                }

                var context = HttpContext.RequestServices.GetRequiredService<AgendaIR.Data.ApplicationDbContext>();

                var funcionario = await context.Funcionarios
                    .FirstOrDefaultAsync(f => f.GoogleCalendarEmail == email);

                if (funcionario == null)
                {
                    return Json(new { authorized = false, message = "Funcionário não encontrado" });
                }

                var temToken = !string.IsNullOrEmpty(funcionario.GoogleCalendarToken);

                return Json(new
                {
                    authorized = temToken,
                    email = email,
                    message = temToken ? "Autorizado" : "Não autorizado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar status OAuth");

                return Json(new { authorized = false, message = "Erro ao verificar status" });
            }
        }
    }
}