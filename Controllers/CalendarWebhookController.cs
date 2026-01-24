using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller para receber notificações do Google Calendar via webhook
    /// </summary>
    [ApiController]
    [Route("api/webhook")]
    public class CalendarWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarWebhookController> _logger;

        public CalendarWebhookController(
            ApplicationDbContext context,
            ILogger<CalendarWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint POST para receber notificações do Google Calendar
        /// </summary>
        [HttpPost("calendar")]
        public IActionResult CalendarNotification()
        {
            try
            {
                // Ler headers do Google Calendar
                var channelId = Request.Headers["X-Goog-Channel-ID"].ToString();
                var resourceState = Request.Headers["X-Goog-Resource-State"].ToString();
                var resourceId = Request.Headers["X-Goog-Resource-ID"].ToString();
                var channelExpiration = Request.Headers["X-Goog-Channel-Expiration"].ToString();

                _logger.LogInformation($"📩 Webhook recebido do Google Calendar");
                _logger.LogInformation($"   Channel ID: {channelId}");
                _logger.LogInformation($"   Resource State: {resourceState}");
                _logger.LogInformation($"   Resource ID: {resourceId}");
                _logger.LogInformation($"   Expiration: {channelExpiration}");

                // Processar notificação baseado no estado
                switch (resourceState.ToLower())
                {
                    case "sync":
                        _logger.LogInformation("Sincronização inicial do canal");
                        break;

                    case "exists":
                        _logger.LogInformation("Evento modificado ou criado");
                        // Aqui você pode buscar detalhes do evento e atualizar o banco
                        break;

                    case "not_exists":
                        _logger.LogInformation("Evento deletado");
                        // Aqui você pode marcar o agendamento como cancelado
                        break;

                    default:
                        _logger.LogWarning($"Estado desconhecido: {resourceState}");
                        break;
                }

                // Retornar 200 OK para confirmar recebimento
                return Ok(new { message = "Webhook processado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook do Google Calendar");
                // Retornar 200 mesmo com erro para evitar reenvios
                return Ok(new { message = "Webhook recebido mas erro ao processar" });
            }
        }

        /// <summary>
        /// Endpoint GET para verificar se o webhook está funcionando
        /// </summary>
        [HttpGet("calendar/health")]
        public IActionResult Health()
        {
            return Ok(new 
            { 
                status = "healthy",
                timestamp = DateTime.UtcNow,
                message = "Webhook endpoint está ativo e pronto para receber notificações"
            });
        }
    }
}
