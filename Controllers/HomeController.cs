using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgendaIR.Models;

namespace AgendaIR.Controllers;

/// <summary>
/// Controller principal da aplicação
/// Gerencia a página inicial e navegação básica
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Página inicial do sistema
    /// Redireciona para diferentes áreas dependendo do tipo de usuário
    /// </summary>
    public IActionResult Index()
    {
        // Se usuário está autenticado, redirecionar para a área apropriada
        if (User.Identity?.IsAuthenticated == true)
        {
            var userType = User.FindFirst("UserType")?.Value;
            
            // Cliente vai direto para seus agendamentos
            if (userType == "Cliente")
            {
                return RedirectToAction("MeusAgendamentos", "Agendamentos");
            }
            
            // Funcionário/Admin vão para lista de agendamentos
            return RedirectToAction("Index", "Agendamentos");
        }

        // Se não está autenticado, mostrar página de boas-vindas
         return RedirectToAction("Login", "Auth");
    }

    /// <summary>
    /// Página de privacidade
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Página de erro
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
