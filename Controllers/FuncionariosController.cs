using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de funcionários (CRUD completo)
    /// Apenas administradores podem acessar estas funcionalidades
    /// </summary>
    [Authorize] // Requer que o usuário esteja autenticado
    public class FuncionariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FuncionariosController> _logger;

        public FuncionariosController(ApplicationDbContext context, ILogger<FuncionariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verifica se o usuário atual é administrador
        /// Retorna true se for admin, false caso contrário
        /// </summary>
        private bool IsUserAdmin()
        {
            var isAdminClaim = User.FindFirst("IsAdmin")?.Value;
            return isAdminClaim != null && bool.Parse(isAdminClaim);
        }

        // GET: Funcionarios
        /// <summary>
        /// Lista todos os funcionários cadastrados no sistema
        /// Apenas administradores podem visualizar esta lista
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou acessar lista de funcionários sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Buscar todos os funcionários ordenados por nome
            var funcionarios = await _context.Funcionarios
                .OrderBy(f => f.Nome)
                .ToListAsync();

            return View(funcionarios);
        }

        // GET: Funcionarios/Details/5
        /// <summary>
        /// Exibe os detalhes de um funcionário específico
        /// Apenas administradores podem visualizar
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        public async Task<IActionResult> Details(int? id)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou acessar detalhes de funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Validar se o ID foi fornecido
            if (id == null)
            {
                return NotFound();
            }

            // Buscar funcionário incluindo seus relacionamentos
            var funcionario = await _context.Funcionarios
                .Include(f => f.Clientes) // Incluir clientes associados
                .Include(f => f.Agendamentos) // Incluir agendamentos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (funcionario == null)
            {
                return NotFound();
            }

            return View(funcionario);
        }

        // GET: Funcionarios/Create
        /// <summary>
        /// Exibe o formulário de criação de um novo funcionário
        /// Apenas administradores podem criar funcionários
        /// </summary>
        public IActionResult Create()
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou criar funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            return View();
        }

        // POST: Funcionarios/Create
        /// <summary>
        /// Processa a criação de um novo funcionário
        /// Valida os dados e criptografa a senha usando BCrypt
        /// </summary>
        /// <param name="model">Dados do novo funcionário</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuncionarioCreateViewModel model)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou criar funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (ModelState.IsValid)
            {
                // Verificar se o username já existe
                var usernameExiste = await _context.Funcionarios
                    .AnyAsync(f => f.Username == model.Username);

                if (usernameExiste)
                {
                    ModelState.AddModelError("Username", "Este nome de usuário já está em uso");
                    return View(model);
                }

                // Verificar se o email já existe
                var emailExiste = await _context.Funcionarios
                    .AnyAsync(f => f.Email == model.Email);

                if (emailExiste)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está em uso");
                    return View(model);
                }

                // Criar novo funcionário
                var funcionario = new Funcionario
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Username = model.Username,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha), // Criptografar senha
                    CPF = model.CPF,
                    GoogleCalendarEmail = model.GoogleCalendarEmail,
                    IsAdmin = model.IsAdmin,
                    Ativo = model.Ativo,
                    DataCriacao = DateTime.UtcNow
                };

                try
                {
                    _context.Add(funcionario);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Funcionário {funcionario.Username} criado com sucesso por {User.Identity?.Name}");
                    TempData["Success"] = "Funcionário criado com sucesso!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar funcionário");
                    ModelState.AddModelError("", "Erro ao criar funcionário. Por favor, tente novamente.");
                }
            }

            return View(model);
        }

        // GET: Funcionarios/Edit/5
        /// <summary>
        /// Exibe o formulário de edição de um funcionário existente
        /// Apenas administradores podem editar funcionários
        /// </summary>
        /// <param name="id">ID do funcionário a ser editado</param>
        public async Task<IActionResult> Edit(int? id)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou editar funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var funcionario = await _context.Funcionarios.FindAsync(id);
            if (funcionario == null)
            {
                return NotFound();
            }

            // Mapear funcionário para ViewModel
            var model = new FuncionarioEditViewModel
            {
                Id = funcionario.Id,
                Nome = funcionario.Nome,
                Email = funcionario.Email,
                Username = funcionario.Username,
                CPF = funcionario.CPF,
                GoogleCalendarEmail = funcionario.GoogleCalendarEmail,
                IsAdmin = funcionario.IsAdmin,
                Ativo = funcionario.Ativo
            };

            return View(model);
        }

        // POST: Funcionarios/Edit/5
        /// <summary>
        /// Processa a edição de um funcionário existente
        /// Permite atualizar dados e opcionalmente alterar a senha
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <param name="model">Dados atualizados do funcionário</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FuncionarioEditViewModel model)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou editar funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Buscar funcionário existente
                var funcionario = await _context.Funcionarios.FindAsync(id);
                if (funcionario == null)
                {
                    return NotFound();
                }

                // Verificar se o username já existe em outro funcionário
                var usernameExiste = await _context.Funcionarios
                    .AnyAsync(f => f.Username == model.Username && f.Id != id);

                if (usernameExiste)
                {
                    ModelState.AddModelError("Username", "Este nome de usuário já está em uso");
                    return View(model);
                }

                // Verificar se o email já existe em outro funcionário
                var emailExiste = await _context.Funcionarios
                    .AnyAsync(f => f.Email == model.Email && f.Id != id);

                if (emailExiste)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está em uso");
                    return View(model);
                }

                try
                {
                    // Atualizar dados do funcionário
                    funcionario.Nome = model.Nome;
                    funcionario.Email = model.Email;
                    funcionario.Username = model.Username;
                    funcionario.CPF = model.CPF;
                    funcionario.GoogleCalendarEmail = model.GoogleCalendarEmail;
                    funcionario.IsAdmin = model.IsAdmin;
                    funcionario.Ativo = model.Ativo;

                    // Se uma nova senha foi fornecida, atualizar
                    if (!string.IsNullOrWhiteSpace(model.NovaSenha))
                    {
                        funcionario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.NovaSenha);
                        _logger.LogInformation($"Senha do funcionário {funcionario.Username} foi alterada");
                    }

                    _context.Update(funcionario);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Funcionário {funcionario.Username} atualizado com sucesso por {User.Identity?.Name}");
                    TempData["Success"] = "Funcionário atualizado com sucesso!";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FuncionarioExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar funcionário");
                    ModelState.AddModelError("", "Erro ao atualizar funcionário. Por favor, tente novamente.");
                }
            }

            return View(model);
        }

        // GET: Funcionarios/Delete/5
        /// <summary>
        /// Exibe a página de confirmação de exclusão de um funcionário
        /// Apenas administradores podem excluir funcionários
        /// </summary>
        /// <param name="id">ID do funcionário a ser excluído</param>
        public async Task<IActionResult> Delete(int? id)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou excluir funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var funcionario = await _context.Funcionarios
                .Include(f => f.Clientes)
                .Include(f => f.Agendamentos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (funcionario == null)
            {
                return NotFound();
            }

            return View(funcionario);
        }

        // POST: Funcionarios/Delete/5
        /// <summary>
        /// Processa a exclusão de um funcionário
        /// Verifica se o funcionário possui clientes ou agendamentos associados antes de excluir
        /// </summary>
        /// <param name="id">ID do funcionário a ser excluído</param>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Verificar se o usuário é administrador
            if (!IsUserAdmin())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou excluir funcionário sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            var funcionario = await _context.Funcionarios
                .Include(f => f.Clientes)
                .Include(f => f.Agendamentos)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (funcionario == null)
            {
                return NotFound();
            }

            // Verificar se o funcionário possui clientes
            if (funcionario.Clientes.Any())
            {
                TempData["Error"] = "Não é possível excluir este funcionário pois ele possui clientes associados. Desative-o ao invés de excluir.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Verificar se o funcionário possui agendamentos
            if (funcionario.Agendamentos.Any())
            {
                TempData["Error"] = "Não é possível excluir este funcionário pois ele possui agendamentos associados. Desative-o ao invés de excluir.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Funcionarios.Remove(funcionario);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Funcionário {funcionario.Username} excluído com sucesso por {User.Identity?.Name}");
                TempData["Success"] = "Funcionário excluído com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir funcionário");
                TempData["Error"] = "Erro ao excluir funcionário. Por favor, tente novamente.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Verifica se um funcionário existe no banco de dados
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>True se existe, False caso contrário</returns>
        private bool FuncionarioExists(int id)
        {
            return _context.Funcionarios.Any(e => e.Id == id);
        }
    }
}
