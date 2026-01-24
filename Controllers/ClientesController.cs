using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;
using AgendaIR.Services;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de clientes (CRUD completo)
    /// Acessível por Funcionários e Administradores
    /// Funcionários veem apenas seus próprios clientes
    /// Administradores veem todos os clientes e podem filtrar por funcionário
    /// </summary>
    [Authorize] // Requer que o usuário esteja autenticado
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MagicLinkService _magicLinkService;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            ApplicationDbContext context,
            MagicLinkService magicLinkService,
            ILogger<ClientesController> logger)
        {
            _context = context;
            _magicLinkService = magicLinkService;
            _logger = logger;
        }

        /// <summary>
        /// Verifica se o usuário atual é administrador
        /// </summary>
        private bool IsUserAdmin()
        {
            var isAdminClaim = User.FindFirst("IsAdmin")?.Value;
            return isAdminClaim != null && bool.Parse(isAdminClaim);
        }

        /// <summary>
        /// Verifica se o usuário atual é um funcionário
        /// </summary>
        private bool IsUserFuncionario()
        {
            var userType = User.FindFirst("UserType")?.Value;
            return userType == "Funcionario";
        }

        /// <summary>
        /// Obtém o ID do funcionário logado
        /// </summary>
        private int? GetCurrentFuncionarioId()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int funcionarioId))
            {
                return funcionarioId;
            }
            return null;
        }

        /// <summary>
        /// Verifica se o usuário tem permissão para acessar funcionalidades de clientes
        /// Apenas funcionários e administradores podem acessar
        /// </summary>
        private bool HasPermission()
        {
            return IsUserFuncionario() || IsUserAdmin();
        }

        // GET: Clientes
        /// <summary>
        /// Lista os clientes
        /// - Funcionário: vê apenas seus próprios clientes
        /// - Admin: vê todos os clientes, com opção de filtrar por funcionário
        /// </summary>
        public async Task<IActionResult> Index(int? funcionarioId)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou acessar clientes sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Query base
            var query = _context.Clientes
                .Include(c => c.Funcionario)
                .AsQueryable();

            // Se for funcionário (não admin), mostrar apenas seus clientes
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
                query = query.Where(c => c.FuncionarioId == currentFuncionarioId.Value);
            }
            else if (funcionarioId.HasValue) // Admin com filtro por funcionário
            {
                query = query.Where(c => c.FuncionarioId == funcionarioId.Value);
            }

            // Ordenar por nome
            var clientes = await query
                .OrderBy(c => c.Nome)
                .ToListAsync();

            // Se for admin, carregar lista de funcionários para o filtro
            if (IsUserAdmin())
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
                ViewBag.FuncionarioIdFiltro = funcionarioId;
            }

            // Passar a URL base para gerar magic links na view
            ViewBag.BaseUrl = $"{Request.Scheme}://{Request.Host}";

            return View(clientes);
        }

        // GET: Clientes/Details/5
        /// <summary>
        /// Exibe detalhes de um cliente específico
        /// Funcionário só pode ver detalhes de seus próprios clientes
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar se funcionário tem permissão para ver este cliente
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    _logger.LogWarning($"Funcionário {User.Identity?.Name} tentou acessar cliente {id} sem permissão");
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            // Gerar magic link para exibição
            ViewBag.MagicLink = _magicLinkService.GerarMagicLink(
                cliente.MagicToken,
                $"{Request.Scheme}://{Request.Host}");

            return View(cliente);
        }

        // GET: Clientes/Create
        /// <summary>
        /// Exibe formulário para criar novo cliente
        /// </summary>
        public async Task<IActionResult> Create()
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var model = new ClienteCreateViewModel();

            // Se for funcionário, preencher automaticamente com seu próprio ID
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
                model.FuncionarioId = currentFuncionarioId.Value;
            }

            // Se for admin, carregar lista de funcionários para seleção
            if (IsUserAdmin())
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
            }

            return View(model);
        }

        // POST: Clientes/Create
        /// <summary>
        /// Processa criação de novo cliente
        /// Gera automaticamente o MagicToken
        /// Redireciona para página de sucesso mostrando o magic link
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteCreateViewModel model)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Se for funcionário (não admin), garantir que só pode criar cliente para si mesmo
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || model.FuncionarioId != currentFuncionarioId.Value)
                {
                    _logger.LogWarning($"Funcionário tentou criar cliente para outro funcionário");
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            if (ModelState.IsValid)
            {
                // Verificar se funcionário existe e está ativo
                var funcionario = await _context.Funcionarios
                    .FirstOrDefaultAsync(f => f.Id == model.FuncionarioId && f.Ativo);

                if (funcionario == null)
                {
                    ModelState.AddModelError("FuncionarioId", "Funcionário não encontrado ou inativo");
                    
                    // Recarregar ViewBag se for admin
                    if (IsUserAdmin())
                    {
                        ViewBag.Funcionarios = await _context.Funcionarios
                            .Where(f => f.Ativo)
                            .OrderBy(f => f.Nome)
                            .ToListAsync();
                    }
                    
                    return View(model);
                }

                // Verificar se já existe cliente com mesmo CPF
                var cpfExistente = await _context.Clientes
                    .AnyAsync(c => c.CPF == model.CPF);

                if (cpfExistente)
                {
                    ModelState.AddModelError("CPF", "Já existe um cliente cadastrado com este CPF");
                    
                    // Recarregar ViewBag se for admin
                    if (IsUserAdmin())
                    {
                        ViewBag.Funcionarios = await _context.Funcionarios
                            .Where(f => f.Ativo)
                            .OrderBy(f => f.Nome)
                            .ToListAsync();
                    }
                    
                    return View(model);
                }

                // Gerar MagicToken único
                var magicToken = _magicLinkService.GerarMagicToken();

                // Criar novo cliente
                var cliente = new Cliente
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Telefone = model.Telefone,
                    TelefoneResidencial = model.TelefoneResidencial,
                    TelefoneComercial = model.TelefoneComercial,
                    Observacoes = model.Observacoes,
                    CorDaPasta = model.CorDaPasta,
                    CPF = model.CPF,
                    FuncionarioId = model.FuncionarioId,
                    MagicToken = magicToken,
                    TokenGeradoEm = DateTime.UtcNow,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Add(cliente);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Novo cliente criado: {cliente.Nome} (ID: {cliente.Id}) por {User.Identity?.Name}");

                // Redirecionar para página de sucesso com o ID do cliente
                return RedirectToAction(nameof(CreatedSuccess), new { id = cliente.Id });
            }

            // Se chegou aqui, houve erro de validação
            // Recarregar ViewBag se for admin
            if (IsUserAdmin())
            {
                ViewBag.Funcionarios = await _context.Funcionarios
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .ToListAsync();
            }

            return View(model);
        }

        // GET: Clientes/CreatedSuccess/5
        /// <summary>
        /// Exibe página de sucesso após criação do cliente
        /// Mostra o magic link gerado para copiar e compartilhar
        /// </summary>
        public async Task<IActionResult> CreatedSuccess(int? id)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar permissão
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            // Gerar magic link completo
            var magicLink = _magicLinkService.GerarMagicLink(
                cliente.MagicToken,
                $"{Request.Scheme}://{Request.Host}");

            ViewBag.MagicLink = magicLink;

            // Gerar link do WhatsApp
            // Remove caracteres não numéricos do telefone
            var telefoneNumeros = new string(cliente.Telefone.Where(char.IsDigit).ToArray());
            var mensagemWhatsApp = Uri.EscapeDataString($"Olá {cliente.Nome}! Aqui está seu link de acesso ao sistema: {magicLink}");
            ViewBag.WhatsAppLink = $"https://wa.me/{telefoneNumeros}?text={mensagemWhatsApp}";

            return View(cliente);
        }

        // GET: Clientes/Edit/5
        /// <summary>
        /// Exibe formulário para editar cliente existente
        /// FuncionarioId não pode ser alterado
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar permissão
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    _logger.LogWarning($"Funcionário {User.Identity?.Name} tentou editar cliente {id} sem permissão");
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            // Mapear para ViewModel
            var model = new ClienteEditViewModel
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                TelefoneResidencial = cliente.TelefoneResidencial,
                TelefoneComercial = cliente.TelefoneComercial,
                Observacoes = cliente.Observacoes,
                CorDaPasta = cliente.CorDaPasta,
                CPF = cliente.CPF,
                Ativo = cliente.Ativo,
                FuncionarioId = cliente.FuncionarioId,
                FuncionarioNome = cliente.Funcionario?.Nome
            };

            return View(model);
        }

        // POST: Clientes/Edit/5
        /// <summary>
        /// Processa edição de cliente
        /// FuncionarioId é IMUTÁVEL e não pode ser alterado
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClienteEditViewModel model)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            // Buscar cliente original
            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar permissão
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            if (ModelState.IsValid)
            {
                // Verificar se CPF já existe para outro cliente
                var cpfExistente = await _context.Clientes
                    .AnyAsync(c => c.CPF == model.CPF && c.Id != id);

                if (cpfExistente)
                {
                    ModelState.AddModelError("CPF", "Já existe outro cliente cadastrado com este CPF");
                    model.FuncionarioNome = cliente.Funcionario?.Nome;
                    return View(model);
                }

                try
                {
                    // Atualizar apenas campos permitidos
                    cliente.Nome = model.Nome;
                    cliente.Email = model.Email;
                    cliente.Telefone = model.Telefone;
                    cliente.TelefoneResidencial = model.TelefoneResidencial;
                    cliente.TelefoneComercial = model.TelefoneComercial;
                    cliente.Observacoes = model.Observacoes;
                    cliente.CorDaPasta = model.CorDaPasta;
                    cliente.CPF = model.CPF;
                    cliente.Ativo = model.Ativo;
                    
                    // FuncionarioId NÃO é atualizado - é IMUTÁVEL
                    // MagicToken também NÃO é alterado

                    _context.Update(cliente);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Cliente {cliente.Id} atualizado por {User.Identity?.Name}");

                    TempData["Success"] = "Cliente atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClienteExists(cliente.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Se chegou aqui, houve erro - recarregar nome do funcionário
            model.FuncionarioNome = cliente.Funcionario?.Nome;
            return View(model);
        }

        // GET: Clientes/Delete/5
        /// <summary>
        /// Exibe confirmação para deletar cliente
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Funcionario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar permissão
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        /// <summary>
        /// Confirma e executa deleção do cliente
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var cliente = await _context.Clientes.FindAsync(id);
            
            if (cliente == null)
            {
                return NotFound();
            }

            // Verificar permissão
            if (!IsUserAdmin())
            {
                var currentFuncionarioId = GetCurrentFuncionarioId();
                if (currentFuncionarioId == null || cliente.FuncionarioId != currentFuncionarioId.Value)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cliente {id} deletado por {User.Identity?.Name}");

            TempData["Success"] = "Cliente deletado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Verifica se cliente existe
        /// </summary>
        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }

        /// <summary>
        /// API: Buscar clientes por nome ou CPF (autocomplete)
        /// </summary>
        [HttpGet("/api/clientes/buscar")]
        public async Task<IActionResult> BuscarClientes(string termo)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            var userType = User.FindFirst("UserType")?.Value;
            if (userType != "Funcionario")
            {
                return Forbid();
            }

            var funcionarioId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

            if (string.IsNullOrWhiteSpace(termo) || termo.Length < 2)
            {
                return Json(new List<object>());
            }

            // Sanitize input - remove any special characters that could cause issues
            termo = termo.Trim();

            var query = _context.Clientes
                .Where(c => c.Ativo)
                .AsQueryable();

            // Se não for admin, filtrar apenas clientes do funcionário
            if (!isAdmin)
            {
                query = query.Where(c => c.FuncionarioId == funcionarioId);
            }

            // Buscar por nome ou CPF (EF Core automatically parameterizes these queries)
            var termoLower = termo.ToLower();
            query = query.Where(c => 
                c.Nome.ToLower().Contains(termoLower) || 
                c.CPF.Contains(termo));

            var clientes = await query
                .OrderBy(c => c.Nome)
                .Take(10) // Limitar a 10 resultados
                .Select(c => new
                {
                    id = c.Id,
                    nome = c.Nome,
                    cpf = c.CPF,
                    telefone = c.Telefone,
                    label = c.Nome + " - " + c.CPF
                })
                .ToListAsync();

            return Json(clientes);
        }

        /// <summary>
        /// Converte nome da cor para código hexadecimal
        /// </summary>
        private string GetCorHex(string? corNome)
        {
            if (string.IsNullOrEmpty(corNome))
                return "#6c757d";

            return corNome.ToLower() switch
            {
                "verde" => "#28a745",
                "azul" => "#007bff",
                "amarelo" => "#ffc107",
                "vermelho" => "#dc3545",
                "rosa" => "#e83e8c",
                "roxo" => "#6f42c1",
                "laranja" => "#fd7e14",
                "preto" => "#343a40",
                "branco" => "#f8f9fa",
                "marrom" => "#795548",
                "cinza" => "#6c757d",
                _ => "#6c757d"
            };
        }
    }
}
