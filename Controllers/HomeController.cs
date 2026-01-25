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
    /// Admin vê dados globais, Funcionário vê apenas seus dados
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

        // Detectar se é Admin
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        
        // Pegar ID do funcionário logado
        var funcionarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var funcionarioId = 0;
        if (funcionarioIdClaim != null)
        {
            int.TryParse(funcionarioIdClaim, out funcionarioId);
        }

        // Criar ViewModel com dados da dashboard
        var viewModel = new DashboardViewModel();

        // ✅ NOVO: Informações do usuário
        viewModel.IsAdmin = isAdmin;
        viewModel.NomeFuncionario = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Usuário";

        // ===== KPIs =====
        
        IQueryable<Agendamento> agendamentosQuery = _context.Agendamentos;
        IQueryable<Cliente> clientesQuery = _context.Clientes.Where(c => c.Ativo);

        // ✅ FILTRAR POR FUNCIONÁRIO SE NÃO FOR ADMIN
        if (!isAdmin && funcionarioId > 0)
        {
            agendamentosQuery = agendamentosQuery.Where(a => a.FuncionarioId == funcionarioId);
            clientesQuery = clientesQuery.Where(c => c.FuncionarioResponsavelId == funcionarioId);
        }

        // Total de agendamentos
        viewModel.TotalAgendamentos = await agendamentosQuery.CountAsync();

        // Total de clientes ativos
        viewModel.TotalClientes = await clientesQuery.CountAsync();

        // Total de funcionários ativos (APENAS ADMIN)
        if (isAdmin)
        {
            viewModel.TotalFuncionarios = await _context.Funcionarios.CountAsync(f => f.Ativo);
        }
        else
        {
            viewModel.TotalFuncionarios = 0; // Funcionário não vê este dado
        }

        // ===== Agendamentos por Status =====
        viewModel.AgendamentosConcluidos = await agendamentosQuery
            .CountAsync(a => a.Status.ToLower() == "concluído" || a.Status.ToLower() == "concluido");

        viewModel.AgendamentosPendentes = await agendamentosQuery
            .CountAsync(a => a.Status.ToLower() == "pendente");

        viewModel.AgendamentosConfirmados = await agendamentosQuery
            .CountAsync(a => a.Status.ToLower() == "confirmado");

        viewModel.AgendamentosCancelados = await agendamentosQuery
            .CountAsync(a => a.Status.ToLower() == "cancelado");

        // Taxa de conclusão
        viewModel.TaxaConclusao = viewModel.TotalAgendamentos > 0
            ? Math.Round((decimal)viewModel.AgendamentosConcluidos / viewModel.TotalAgendamentos * 100, 1)
            : 0;

        // ===== Top Status =====
        viewModel.TopStatus = await agendamentosQuery
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
        var agendamentosPorDia = await agendamentosQuery
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
