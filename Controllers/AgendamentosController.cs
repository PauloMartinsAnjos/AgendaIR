using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;
using AgendaIR.Services;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar agendamentos
    /// Implementa funcionalidades diferentes para Clientes, Funcionários e Administradores
    /// </summary>
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
        /// Obtém o ID do usuário logado da sessão
        /// </summary>
        private int? GetUsuarioId()
        {
            return HttpContext.Session.GetInt32("UsuarioId");
        }

        /// <summary>
        /// Obtém o tipo de usuário logado (Cliente, Funcionario)
        /// </summary>
        private string? GetUserType()
        {
            return HttpContext.Session.GetString("UserType");
        }

        /// <summary>
        /// Verifica se o usuário logado é admin
        /// </summary>
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }

        /// <summary>
        /// Valida se a data/hora do agendamento está dentro das regras de negócio
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidarDataHoraAgendamento(DateTime dataHora)
        {
            // Verificar se a data é futura
            if (dataHora <= DateTime.Now)
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

            // Recarregar lista de documentos para exibir em caso de erro
            model.Documentos = await _context.DocumentosSolicitados
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
            var documentosObrigatorios = await _context.DocumentosSolicitados
                .Where(d => d.Ativo && d.Obrigatorio)
                .Select(d => d.Id)
                .ToListAsync();

            foreach (var docId in documentosObrigatorios)
            {
                var documento = model.Documentos.FirstOrDefault(d => d.DocumentoSolicitadoId == docId);
                if (documento?.Arquivo == null || documento.Arquivo.Length == 0)
                {
                    var nomeDoc = await _context.DocumentosSolicitados
                        .Where(d => d.Id == docId)
                        .Select(d => d.Nome)
                        .FirstOrDefaultAsync();
                    ModelState.AddModelError("", $"O documento '{nomeDoc}' é obrigatório");
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

            // Criar evento no Google Calendar
            var eventId = await _calendarService.CriarEventoAsync(
                cliente.Funcionario?.GoogleCalendarEmail ?? "",
                cliente.Nome,
                model.DataHora
            );

            if (eventId != null)
            {
                agendamento.GoogleCalendarEventId = eventId;
                await _context.SaveChangesAsync();
            }

            // Fazer upload dos documentos anexados
            foreach (var documento in model.Documentos)
            {
                if (documento.Arquivo != null && documento.Arquivo.Length > 0)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(
                        documento.Arquivo,
                        agendamento.Id
                    );

                    if (uploadResult.Success)
                    {
                        var documentoAnexado = new DocumentoAnexado
                        {
                            AgendamentoId = agendamento.Id,
                            DocumentoSolicitadoId = documento.DocumentoSolicitadoId,
                            NomeArquivo = documento.Arquivo.FileName,
                            CaminhoArquivo = uploadResult.FilePath!,
                            TamanhoBytes = documento.Arquivo.Length,
                            DataUpload = DateTime.UtcNow
                        };

                        _context.DocumentosAnexados.Add(documentoAnexado);
                    }
                    else
                    {
                        _logger.LogError($"Erro ao fazer upload do arquivo: {uploadResult.ErrorMessage}");
                    }
                }
            }

            await _context.SaveChangesAsync();

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
            var horasRestantes = (agendamento.DataHora - DateTime.Now).TotalHours;
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

            // Obter caminho completo do arquivo
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", documento.CaminhoArquivo);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Arquivo não encontrado no servidor.";
                return RedirectToAction(nameof(Details), new { id = documento.AgendamentoId });
            }

            // Retornar arquivo para download
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(documento.NomeArquivo);

            return File(fileBytes, contentType, documento.NomeArquivo);
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
