using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;

namespace AgendaIR.Controllers;

/// <summary>
/// Controller principal da aplicação
/// Gerencia a página inicial e navegação básica
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Página inicial do sistema
    /// Redireciona para diferentes áreas dependendo do tipo de usuário
    /// </summary>
    public IActionResult Index()
    {
        // Se usuário está autenticado, mostrar dashboard ou redirecionar
        if (User.Identity?.IsAuthenticated == true)
        {
            var userType = User.FindFirst("UserType")?.Value;
            
            // Cliente vai direto para seus agendamentos
            if (userType == "Cliente")
            {
                return RedirectToAction("MeusAgendamentos", "Agendamentos");
            }
            
            // Funcionário/Admin vão para a dashboard
            return RedirectToAction("Dashboard");
        }

        // Se não está autenticado, mostrar página de boas-vindas
         return RedirectToAction("Login", "Auth");
    }

    /// <summary>
    /// Dashboard com estatísticas e gráficos do sistema
    /// Disponível apenas para funcionários e administradores
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var userType = User.FindFirst("UserType")?.Value;
        
        // Clientes não têm acesso à dashboard
        if (userType == "Cliente")
        {
            return RedirectToAction("MeusAgendamentos", "Agendamentos");
        }

        // Criar ViewModel com dados da dashboard
        var viewModel = new DashboardViewModel();

        // ===== KPIs =====
        // Total de agendamentos
        viewModel.TotalAgendamentos = await _context.Agendamentos.CountAsync();

        // Total de clientes ativos
        viewModel.TotalClientes = await _context.Clientes.CountAsync(c => c.Ativo);

        // Total de funcionários ativos
        viewModel.TotalFuncionarios = await _context.Funcionarios.CountAsync(f => f.Ativo);

        // ===== Agendamentos por Status =====
        viewModel.AgendamentosConcluidos = await _context.Agendamentos
            .CountAsync(a => a.Status.ToLower() == "concluído" || a.Status.ToLower() == "concluido");

        viewModel.AgendamentosPendentes = await _context.Agendamentos
            .CountAsync(a => a.Status.ToLower() == "pendente");

        viewModel.AgendamentosConfirmados = await _context.Agendamentos
            .CountAsync(a => a.Status.ToLower() == "confirmado");

        viewModel.AgendamentosCancelados = await _context.Agendamentos
            .CountAsync(a => a.Status.ToLower() == "cancelado");

        // Taxa de conclusão
        viewModel.TaxaConclusao = viewModel.TotalAgendamentos > 0
            ? Math.Round((decimal)viewModel.AgendamentosConcluidos / viewModel.TotalAgendamentos * 100, 1)
            : 0;

        // ===== Top Status =====
        viewModel.TopStatus = await _context.Agendamentos
            .GroupBy(a => a.Status)
            .Select(g => new StatusCount
            {
                Nome = g.Key,
                Quantidade = g.Count()
            })
            .OrderByDescending(s => s.Quantidade)
            .Take(5)
            .ToListAsync();

        // ===== Últimos 7 Dias =====
        var seteDiasAtras = DateTime.UtcNow.AddDays(-7).Date;
        var agendamentosPorDia = await _context.Agendamentos
            .Where(a => a.DataCriacao >= seteDiasAtras)
            .GroupBy(a => a.DataCriacao.Date)
            .Select(g => new
            {
                Data = g.Key,
                Quantidade = g.Count()
            })
            .OrderBy(d => d.Data)
            .ToListAsync();

        // Preencher todos os 7 dias (incluindo dias sem agendamentos)
        viewModel.UltimosSeteDias = new List<AgendamentoDia>();
        for (int i = 6; i >= 0; i--)
        {
            var dia = DateTime.UtcNow.AddDays(-i).Date;
            var agendamentoDia = agendamentosPorDia.FirstOrDefault(a => a.Data == dia);
            
            viewModel.UltimosSeteDias.Add(new AgendamentoDia
            {
                Dia = dia.ToString("dd/MM"),
                Quantidade = agendamentoDia?.Quantidade ?? 0
            });
        }

        return View(viewModel);
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
