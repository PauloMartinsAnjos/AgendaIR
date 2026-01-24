using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgendaIR.Data;
using AgendaIR.Services;
using AgendaIR.Models.ViewModels;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável por autenticação e autorização
    /// Gerencia login de funcionários (com usuário/senha) e clientes (com magic link)
    /// </summary>
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MagicLinkService _magicLinkService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            MagicLinkService magicLinkService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _magicLinkService = magicLinkService;
            _logger = logger;
        }

        // ===== LOGIN DE FUNCIONÁRIOS =====

        /// <summary>
        /// Exibe a página de login para funcionários e administradores
        /// GET: /Auth/Login
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Processa o login de funcionários com usuário e senha
        /// POST: /Auth/Login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Buscar funcionário pelo username
            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Username == model.Username && f.Ativo);

            // Verificar se funcionário existe e senha está correta
            if (funcionario == null || !BCrypt.Net.BCrypt.Verify(model.Password, funcionario.SenhaHash))
            {
                ModelState.AddModelError("", "Usuário ou senha inválidos");
                return View(model);
            }

            // Criar claims (informações do usuário para o cookie de autenticação)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, funcionario.Id.ToString()),
                new Claim(ClaimTypes.Name, funcionario.Nome),
                new Claim(ClaimTypes.Email, funcionario.Email),
                new Claim("Username", funcionario.Username),
                new Claim("UserType", "Funcionario"),
                new Claim("IsAdmin", funcionario.IsAdmin.ToString())
            };

            // Criar identidade
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Criar propriedades de autenticação
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, // Mantém login mesmo após fechar navegador
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            };

            // Fazer login (criar cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"Funcionário {funcionario.Username} fez login com sucesso");

            // Redirecionar para página inicial
            return RedirectToAction("Index", "Home");
        }

        // ===== LOGIN DE CLIENTES (MAGIC LINK) =====

        /// <summary>
        /// Processa login automático de cliente via magic link
        /// GET: /Auth/LoginMagic?token=abc123...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LoginMagic(string? token)
        {
            // Validar token
            if (!_magicLinkService.ValidarToken(token))
            {
                TempData["Error"] = "Link de acesso inválido ou expirado";
                return RedirectToAction("AccessDenied");
            }

            // Buscar cliente pelo token
            var cliente = await _context.Clientes
                .Include(c => c.Funcionario) // Incluir dados do funcionário
                .FirstOrDefaultAsync(c => c.MagicToken == token && c.Ativo);

            if (cliente == null)
            {
                _logger.LogWarning($"⚠️ Token inválido: {token}");
                return View("TokenInvalido");
            }

            // VALIDAR EXPIRAÇÃO
            if (!cliente.TokenValido())
            {
                _logger.LogWarning($"⏰ Token expirado para cliente {cliente.Nome} - Expirou em: {cliente.TokenExpiracao:dd/MM/yyyy HH:mm}");
                
                ViewBag.ClienteNome = cliente.Nome;
                ViewBag.ClienteId = cliente.Id;
                ViewBag.TokenExpiracao = cliente.TokenExpiracao;
                
                return View("TokenExpirado");
            }

            // Token válido - continuar normalmente
            _logger.LogInformation($"✅ Token válido para cliente {cliente.Nome} - Expira em: {cliente.TokenExpiracao:dd/MM/yyyy HH:mm}");

            // Criar claims para o cliente
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
                new Claim(ClaimTypes.Name, cliente.Nome),
                new Claim(ClaimTypes.Email, cliente.Email),
                new Claim("UserType", "Cliente"),
                new Claim("FuncionarioId", cliente.FuncionarioId.ToString()),
                new Claim("IsAdmin", "False")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Cliente sempre fica logado
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // 30 dias
            };

            // Fazer login
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"Cliente {cliente.Nome} fez login via magic link");

            // Redirecionar cliente para página de agendamentos
            return RedirectToAction("MeusAgendamentos", "Agendamentos");
        }

        // ===== LOGOUT =====

        /// <summary>
        /// Faz logout do usuário (funcionário ou cliente)
        /// GET: /Auth/Logout
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Usuário fez logout");
            return RedirectToAction("Login");
        }

        // ===== ACESSO NEGADO =====

        /// <summary>
        /// Página exibida quando usuário tenta acessar recurso sem permissão
        /// GET: /Auth/AccessDenied
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
