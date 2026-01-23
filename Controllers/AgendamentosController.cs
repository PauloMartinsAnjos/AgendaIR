using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;
using AgendaIR.Services;
using Microsoft.AspNetCore.Authorization;

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

        #region Ações para CLIENTES

        /// <summary>
        /// CLIENTE: Lista os agendamentos do cliente logado
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MeusAgendamentos()
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

            // Buscar agendamentos do cliente
            var agendamentos = await _context.Agendamentos
                .Include(a => a.Funcionario)
                .Include(a => a.DocumentosAnexados)
                .Where(a => a.ClienteId == clienteId.Value)
                .OrderByDescending(a => a.DataHora)
                .Select(a => new AgendamentoListItem
                {
                    Id = a.Id,
                    DataHora = a.DataHora,
                    Status = a.Status,
                    ClienteNome = a.Cliente!.Nome,
                    ClienteEmail = a.Cliente.Email,
                    FuncionarioNome = a.Funcionario!.Nome,
                    TotalDocumentos = a.DocumentosAnexados.Count,
                    DataCriacao = a.DataCriacao
                })
                .ToListAsync();

            var viewModel = new AgendamentoIndexViewModel
            {
                Agendamentos = agendamentos
            };

            return View(viewModel);
        }

        /// <summary>
        /// CLIENTE: Exibe formulário para criar novo agendamento
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
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
            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

            if (cliente == null)
            {
                return NotFound();
            }

            // Buscar todos os documentos solicitados ativos
            var documentos = await _context.DocumentosSolicitados
                .Where(d => d.Ativo)
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

            var viewModel = new AgendamentoCreateViewModel
            {
                FuncionarioId = cliente.FuncionarioId,
                FuncionarioNome = cliente.Funcionario?.Nome ?? "Não atribuído",
                Documentos = documentos
            };

            return View(viewModel);
        }

        /// <summary>
        /// CLIENTE: Processa a criação de um novo agendamento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AgendamentoCreateViewModel model)
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
            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(c => c.Id == clienteId.Value);

            if (cliente == null)
            {
                return NotFound();
            }

            // ✅ CORREÇÃO: Guardar referência aos documentos ANTES de recarregar
            var documentosEnviados = model.Documentos;

            // Recarregar informações dos documentos do banco (SEM perder os arquivos)
            var documentosNoBanco = await _context.DocumentosSolicitados
                .Where(d => d.Ativo)
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

            model.FuncionarioNome = cliente.Funcionario?.Nome ?? "Não atribuído";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validar data e hora do agendamento
            var validacao = ValidarDataHoraAgendamento(model.DataHora);
            if (!validacao.IsValid)
            {
                ModelState.AddModelError("DataHora", validacao.ErrorMessage);
                return View(model);
            }

            // Validar que todos os documentos obrigatórios foram enviados
            var documentosObrigatorios = documentosNoBanco.Where(d => d.Obrigatorio).ToList();

            _logger.LogInformation($"Validando {documentosObrigatorios.Count} documentos obrigatórios");

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
                return View(model);
            }

            // Verificar disponibilidade no Google Calendar
            var disponivel = await _calendarService.VerificarDisponibilidadeAsync(
                cliente.Funcionario?.GoogleCalendarEmail ?? "",
                model.DataHora
            );

            if (!disponivel)
            {
                ModelState.AddModelError("DataHora", "Este horário não está disponível. Por favor, escolha outro.");
                return View(model);
            }

            // Criar o agendamento
            var agendamento = new Agendamento
            {
                ClienteId = clienteId.Value,
                FuncionarioId = cliente.FuncionarioId,
                DataHora = model.DataHora,
                Status = "Pendente",
                Observacoes = model.Observacoes,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✓ Agendamento {agendamento.Id} criado com sucesso para {model.DataHora:yyyy-MM-dd HH:mm}");

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

                var eventId = await _calendarService.CriarEventoAsync(
                    funcionarioEmail,
                    cliente.Nome,
                    model.DataHora
                );

                if (eventId != null)
                {
                    agendamento.GoogleCalendarEventId = eventId;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ ========================================");
                    _logger.LogInformation($"✅ SUCESSO! EVENTO CRIADO NO GOOGLE CALENDAR");
                    _logger.LogInformation($"✅ ========================================");
                    _logger.LogInformation($"✅ Event ID: {eventId}");
                    _logger.LogInformation($"✅ Email: {funcionarioEmail}");
                    _logger.LogInformation($"✅ Cliente: {cliente.Nome}");
                    _logger.LogInformation($"✅ Data/Hora: {model.DataHora:yyyy-MM-dd HH:mm}");
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
                    _logger.LogError($"❌ Cliente: {cliente.Nome}");
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
                    ClienteEmail = a.Cliente.Email,
                    FuncionarioNome = a.Funcionario!.Nome,
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

            // Se admin, carregar funcionários para escolher responsável
            if (isAdmin)
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
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
        public async Task<IActionResult> CreateAgendamento(AgendamentoCreateViewModel model, List<IFormFile> documentos)
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
                // Validar documentos obrigatórios
                // Note: For funcionario workflow, we check if any documents are uploaded
                // More granular validation (specific document types) would require UI changes
                var docsObrigatorios = await _context.DocumentosSolicitados
                    .Where(d => d.TipoAgendamentoId == model.TipoAgendamentoId && d.Obrigatorio && d.Ativo)
                    .ToListAsync();

                if (docsObrigatorios.Any() && (documentos == null || !documentos.Any()))
                {
                    var nomesDocs = string.Join(", ", docsObrigatorios.Select(d => d.Nome));
                    ModelState.AddModelError("", $"Este tipo de agendamento requer os seguintes documentos obrigatórios: {nomesDocs}");
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

                // Upload de documentos (simplified for funcionario workflow)
                if (documentos != null && documentos.Any())
                {
                    int documentosSalvos = 0;
                    foreach (var arquivo in documentos)
                    {
                        if (arquivo.Length > 0)
                        {
                            // Processar e comprimir arquivo
                            var uploadResult = await _fileUploadService.ProcessarArquivoAsync(arquivo);

                            if (uploadResult.Success && uploadResult.ConteudoComprimido != null)
                            {
                                // Salvar no banco de dados (comprimido)
                                // Note: Using generic DocumentoSolicitadoId=1 for now
                                // TODO: Future enhancement - allow mapping uploads to specific document types
                                var documentoAnexado = new DocumentoAnexado
                                {
                                    AgendamentoId = agendamento.Id,
                                    DocumentoSolicitadoId = 1, // Generic - maps to first document type
                                    NomeArquivo = arquivo.FileName,
                                    ConteudoComprimido = uploadResult.ConteudoComprimido,
                                    TamanhoOriginalBytes = uploadResult.TamanhoOriginal,
                                    TamanhoComprimidoBytes = uploadResult.TamanhoComprimido,
                                    DataUpload = DateTime.UtcNow
                                };

                                _context.DocumentosAnexados.Add(documentoAnexado);
                                documentosSalvos++;
                                _logger.LogInformation($"Documento {arquivo.FileName} anexado ao agendamento {agendamento.Id}");
                            }
                        }
                    }

                    if (documentosSalvos > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation($"Agendamento {agendamento.Id} criado por funcionário {User.Identity?.Name}");

                TempData["SuccessMessage"] = "Agendamento criado com sucesso!";
                return RedirectToAction(nameof(Details), new { id = agendamento.Id });
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

            var viewModel = new AgendamentoEditViewModel
            {
                Id = agendamento.Id,
                Status = agendamento.Status,
                Observacoes = agendamento.Observacoes,
                DataHora = agendamento.DataHora,
                ClienteNome = agendamento.Cliente?.Nome ?? "",
                ClienteEmail = agendamento.Cliente?.Email ?? "",
                ClienteTelefone = agendamento.Cliente?.Telefone ?? "",
                FuncionarioNome = agendamento.Funcionario?.Nome ?? ""
            };

            return View(viewModel);
        }

        /// <summary>
        /// FUNCIONÁRIO/ADMIN: Processa a edição de um agendamento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AgendamentoEditViewModel model)
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

            // Atualizar campos
            agendamento.Status = model.Status;
            agendamento.Observacoes = model.Observacoes;
            agendamento.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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