using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;

namespace AgendaIR.Services
{
    /// <summary>
    /// Serviço responsável pela integração REAL com o Google Calendar
    /// Usa OAuth 2.0 para autenticação e criação de eventos
    /// </summary>
    public class GoogleCalendarService
    {
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GoogleCalendarService(
            ILogger<GoogleCalendarService> logger,
            IConfiguration configuration,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Verifica se o Google Calendar está configurado
        /// </summary>
        private bool IsConfigured()
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"];

            return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
        }

        /// <summary>
        /// Obtém as credenciais do funcionário ou inicia fluxo de autorização
        /// </summary>
        private async Task<UserCredential?> GetCredentialAsync(string funcionarioEmail)
        {
            if (!IsConfigured())
            {
                _logger.LogWarning("Google Calendar não configurado no appsettings.json");
                return null;
            }

            try
            {
                // Buscar token do funcionário no banco
                var funcionario = await _context.Funcionarios
                    .FirstOrDefaultAsync(f => f.GoogleCalendarEmail == funcionarioEmail);

                if (funcionario == null)
                {
                    _logger.LogWarning($"Funcionário com email {funcionarioEmail} não encontrado");
                    return null;
                }

                // Se já tem token, usar ele
                if (!string.IsNullOrEmpty(funcionario.GoogleCalendarToken))
                {
                    _logger.LogInformation($"Token encontrado para {funcionarioEmail}");

                    var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(
                        funcionario.GoogleCalendarToken
                    );

                    if (tokenResponse != null)
                    {
                        var clientId = _configuration["GoogleCalendar:ClientId"];
                        var clientSecret = _configuration["GoogleCalendar:ClientSecret"];

                        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                        {
                            _logger.LogError("ClientId ou ClientSecret não configurados");
                            return null;
                        }

                        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = new ClientSecrets
                            {
                                ClientId = clientId,
                                ClientSecret = clientSecret
                            },
                            Scopes = new[] { CalendarService.Scope.Calendar }
                        });

                        var credential = new UserCredential(flow, funcionarioEmail, tokenResponse);

                        // ✅ CORRIGIDO: Usar IsStale ao invés de IsExpired
                        if (tokenResponse.IsStale)
                        {
                            _logger.LogInformation("Token expirado, tentando renovar...");

                            if (await credential.RefreshTokenAsync(CancellationToken.None))
                            {
                                // Salvar novo token
                                var novoToken = System.Text.Json.JsonSerializer.Serialize(credential.Token);
                                funcionario.GoogleCalendarToken = novoToken;
                                await _context.SaveChangesAsync();

                                _logger.LogInformation("Token renovado com sucesso!");
                            }
                            else
                            {
                                _logger.LogWarning("Falha ao renovar token, necessária nova autorização");
                                return null;
                            }
                        }

                        return credential;
                    }
                }

                _logger.LogWarning($"Funcionário {funcionarioEmail} não possui token. Necessária autorização OAuth.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter credenciais para {funcionarioEmail}");
                return null;
            }
        }

        /// <summary>
        /// Cria um CalendarService autenticado
        /// </summary>
        private async Task<CalendarService?> GetCalendarServiceAsync(string funcionarioEmail)
        {
            var credential = await GetCredentialAsync(funcionarioEmail);

            if (credential == null)
            {
                return null;
            }

            return new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _configuration["GoogleCalendar:ApplicationName"] ?? "AgendaIR"
            });
        }

        /// <summary>
        /// Cria um evento no Google Calendar do funcionário
        /// Retorna tupla com (EventId, ConferenciaUrl)
        /// </summary>
        public async Task<(string? EventId, string? ConferenciaUrl)> CriarEventoAsync(
            string funcionarioEmail, 
            string clienteNome, 
            DateTime dataHora, 
            int duracao = 60,
            string? tipoNome = null,
            string? tipoDescricao = null,
            List<string>? participantesEmails = null,
            string? local = null,
            bool criarGoogleMeet = false,
            int corCalendario = 6,
            bool bloqueiaHorario = true)
        {
            try
            {
                if (string.IsNullOrEmpty(funcionarioEmail))
                {
                    _logger.LogWarning("Email do funcionário vazio, não é possível criar evento");
                    return (null, null);
                }

                _logger.LogInformation($"📅 Criando evento no Google Calendar para {funcionarioEmail}");

                var service = await GetCalendarServiceAsync(funcionarioEmail);

                if (service == null)
                {
                    _logger.LogWarning($"⚠️ Não foi possível obter CalendarService - Funcionário precisa autorizar OAuth");

                    // Redirecionar para autorização OAuth
                    IniciarFluxoOAuth(funcionarioEmail);

                    return (null, null);
                }

                // ✅ Criar evento com todos os recursos
                var evento = new Event
                {
                    Summary = !string.IsNullOrEmpty(tipoNome) 
                        ? $"{tipoNome} - {clienteNome}" 
                        : $"Agendamento - {clienteNome}",
                    Description = !string.IsNullOrEmpty(tipoDescricao) 
                        ? tipoDescricao 
                        : $"Atendimento para o cliente {clienteNome}",
                    Start = new EventDateTime
                    {
                        DateTimeDateTimeOffset = new DateTimeOffset(dataHora),
                        TimeZone = "America/Sao_Paulo"
                    },
                    End = new EventDateTime
                    {
                        DateTimeDateTimeOffset = new DateTimeOffset(dataHora.AddMinutes(duracao)),
                        TimeZone = "America/Sao_Paulo"
                    },
                    ColorId = corCalendario.ToString(),
                    Transparency = bloqueiaHorario ? "opaque" : "transparent",
                    Reminders = new Event.RemindersData
                    {
                        UseDefault = false,
                        Overrides = new[]
                        {
                            new EventReminder { Method = "email", Minutes = 24 * 60 }, // 1 dia antes
                            new EventReminder { Method = "popup", Minutes = 30 } // 30 min antes
                        }
                    }
                };

                // Adicionar local se fornecido
                if (!string.IsNullOrEmpty(local))
                {
                    evento.Location = local;
                }

                // Adicionar participantes (se houver)
                if (participantesEmails != null && participantesEmails.Any())
                {
                    evento.Attendees = participantesEmails
                        .Where(email => !string.IsNullOrEmpty(email))
                        .Distinct() // Evitar duplicados
                        .Select(email => new EventAttendee
                        {
                            Email = email,
                            ResponseStatus = "needsAction"
                        })
                        .ToList();
                    
                    _logger.LogInformation($"👥 {participantesEmails.Count} participantes adicionados ao evento");
                }

                // Criar Google Meet se solicitado
                if (criarGoogleMeet)
                {
                    evento.ConferenceData = new ConferenceData
                    {
                        CreateRequest = new CreateConferenceRequest
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            ConferenceSolutionKey = new ConferenceSolutionKey
                            {
                                Type = "hangoutsMeet"
                            }
                        }
                    };
                }

                _logger.LogInformation($"🔄 Enviando requisição para Google Calendar API...");

                var request = service.Events.Insert(evento, "primary");
                
                // Se criar Google Meet, precisa especificar conferenceDataVersion
                if (criarGoogleMeet)
                {
                    request.ConferenceDataVersion = 1;
                }

                var createdEvent = await request.ExecuteAsync();

                _logger.LogInformation($"✅ Evento criado com sucesso! ID: {createdEvent.Id}");

                // Extrair URL do Google Meet se criado
                string? conferenciaUrl = null;
                if (criarGoogleMeet && createdEvent.ConferenceData?.EntryPoints != null)
                {
                    var meetEntry = createdEvent.ConferenceData.EntryPoints
                        .FirstOrDefault(e => e.EntryPointType == "video");
                    conferenciaUrl = meetEntry?.Uri;
                    
                    if (!string.IsNullOrEmpty(conferenciaUrl))
                    {
                        _logger.LogInformation($"🎥 Google Meet criado: {conferenciaUrl}");
                    }
                }

                return (createdEvent.Id, conferenciaUrl);
            }
            catch (Google.GoogleApiException gex)
            {
                _logger.LogError($"❌ Erro da API do Google: {gex.Message}");
                _logger.LogError($"   Status Code: {gex.HttpStatusCode}");
                _logger.LogError($"   Error: {gex.Error?.Message}");
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar evento no Google Calendar");
                return (null, null);
            }
        }

        /// <summary>
        /// Atualiza um evento existente no Google Calendar
        /// </summary>
        public async Task<bool> AtualizarEventoAsync(string funcionarioEmail, string eventId, DateTime novaDataHora, int duracao = 60)
        {
            try
            {
                if (string.IsNullOrEmpty(funcionarioEmail) || string.IsNullOrEmpty(eventId))
                {
                    return false;
                }

                _logger.LogInformation($"Atualizando evento {eventId} no Google Calendar");

                var service = await GetCalendarServiceAsync(funcionarioEmail);

                if (service == null)
                {
                    _logger.LogWarning("Não foi possível obter CalendarService");
                    return false;
                }

                var evento = await service.Events.Get("primary", eventId).ExecuteAsync();

                // ✅ CORRIGIDO: Usar DateTimeDateTimeOffset
                if (evento.Start != null)
                {
                    evento.Start.DateTimeDateTimeOffset = new DateTimeOffset(novaDataHora);
                }

                if (evento.End != null)
                {
                    evento.End.DateTimeDateTimeOffset = new DateTimeOffset(novaDataHora.AddMinutes(duracao));
                }

                await service.Events.Update(evento, "primary", eventId).ExecuteAsync();

                _logger.LogInformation($"✅ Evento {eventId} atualizado com sucesso!");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar evento no Google Calendar");
                return false;
            }
        }

        /// <summary>
        /// Deleta um evento do Google Calendar
        /// </summary>
        public async Task<bool> DeletarEventoAsync(string funcionarioEmail, string eventId)
        {
            try
            {
                if (string.IsNullOrEmpty(funcionarioEmail) || string.IsNullOrEmpty(eventId))
                {
                    return false;
                }

                _logger.LogInformation($"Deletando evento {eventId} do Google Calendar");

                var service = await GetCalendarServiceAsync(funcionarioEmail);

                if (service == null)
                {
                    _logger.LogWarning("Não foi possível obter CalendarService");
                    return false;
                }

                await service.Events.Delete("primary", eventId).ExecuteAsync();

                _logger.LogInformation($"✅ Evento {eventId} deletado com sucesso!");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar evento do Google Calendar");
                return false;
            }
        }

        /// <summary>
        /// Verifica disponibilidade no calendário do funcionário
        /// </summary>
        public async Task<bool> VerificarDisponibilidadeAsync(string funcionarioEmail, DateTime dataHora, int duracao = 60)
        {
            try
            {
                if (string.IsNullOrEmpty(funcionarioEmail))
                {
                    return true; // Se não tem email, não verificar (permite agendamento)
                }

                _logger.LogInformation($"Verificando disponibilidade para {funcionarioEmail} em {dataHora}");

                var service = await GetCalendarServiceAsync(funcionarioEmail);

                if (service == null)
                {
                    _logger.LogWarning("Não foi possível obter CalendarService, assumindo disponível");
                    return true; // Se não conseguir verificar, permite
                }

                var request = service.Events.List("primary");

                // ✅ CORRIGIDO: Usar TimeMinDateTimeOffset e TimeMaxDateTimeOffset
                request.TimeMinDateTimeOffset = new DateTimeOffset(dataHora);
                request.TimeMaxDateTimeOffset = new DateTimeOffset(dataHora.AddMinutes(duracao));
                request.SingleEvents = true;

                var events = await request.ExecuteAsync();

                var disponivel = events.Items.Count == 0;

                _logger.LogInformation($"Disponibilidade: {(disponivel ? "LIVRE" : "OCUPADO")} ({events.Items.Count} eventos encontrados)");

                return disponivel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar disponibilidade no Google Calendar");
                return true; // Em caso de erro, permite o agendamento
            }
        }

        /// <summary>
        /// Inicia fluxo OAuth para autorização
        /// </summary>
        private void IniciarFluxoOAuth(string funcionarioEmail)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext não disponível");
                    return;
                }

                var clientId = _configuration["GoogleCalendar:ClientId"];
                var redirectUri = _configuration["GoogleCalendar:RedirectUri"];

                // ✅ CORRIGIDO: Validar antes de usar
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                {
                    _logger.LogError("ClientId ou RedirectUri não configurados");
                    return;
                }

                var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(funcionarioEmail));

                var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                              $"client_id={Uri.EscapeDataString(clientId)}" +
                              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                              $"&response_type=code" +
                              $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}" +
                              $"&state={state}" +
                              $"&access_type=offline" +
                              $"&prompt=consent";

                _logger.LogInformation($"🔐 Redirecionando para autorização OAuth: {authUrl}");

                // Armazenar URL na sessão para redirecionar depois
                httpContext.Session.SetString("OAuthRedirectUrl", authUrl);

                // ✅ CORRIGIDO: Usar .Append ao invés de .Add
                httpContext.Response.Headers.Append("X-OAuth-Required", "true");
                httpContext.Response.Headers.Append("X-OAuth-Url", authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar fluxo OAuth");
            }
        }

        /// <summary>
        /// Processa o código de autorização OAuth retornado pelo Google
        /// </summary>
        public async Task<bool> ProcessarCodigoOAuthAsync(string code, string state)
        {
            try
            {
                var funcionarioEmail = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));

                _logger.LogInformation($"Processando código OAuth para {funcionarioEmail}");

                var clientId = _configuration["GoogleCalendar:ClientId"];
                var clientSecret = _configuration["GoogleCalendar:ClientSecret"];
                var redirectUri = _configuration["GoogleCalendar:RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
                {
                    _logger.LogError("Configurações OAuth incompletas");
                    return false;
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { CalendarService.Scope.Calendar }
                });

                var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    funcionarioEmail,
                    code,
                    redirectUri,
                    CancellationToken.None
                );

                // Salvar token no banco de dados
                var funcionario = await _context.Funcionarios
                    .FirstOrDefaultAsync(f => f.GoogleCalendarEmail == funcionarioEmail);

                if (funcionario != null)
                {
                    funcionario.GoogleCalendarToken = System.Text.Json.JsonSerializer.Serialize(tokenResponse);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ Token OAuth salvo para {funcionarioEmail}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Funcionário {funcionarioEmail} não encontrado no banco");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar código OAuth");
                return false;
            }
        }
    }
}