using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models.ViewModels;
using System.Security.Claims;

namespace AgendaIR.ViewComponents
{
    /// <summary>
    /// ViewComponent para exibir lembretes de agendamentos próximos
    /// </summary>
    public class LembretesAgendamentosViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public LembretesAgendamentosViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Obter tipo de usuário
            var userType = UserClaimsPrincipal?.FindFirst("UserType")?.Value;
            
            // Se não for funcionário, não exibir lembretes
            if (userType != "Funcionario")
            {
                return Content(string.Empty);
            }

            var userId = UserClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = UserClaimsPrincipal?.FindFirst("IsAdmin")?.Value == "True";

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int funcionarioId))
            {
                return Content(string.Empty);
            }

            var agora = DateTime.UtcNow;
            var hoje = agora.Date;
            var em3Dias = hoje.AddDays(3);
            var em5Dias = hoje.AddDays(5);

            // Buscar agendamentos futuros não cancelados
            var query = _context.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Funcionario)
                .Include(a => a.TipoAgendamento)
                .Where(a => a.DataHora >= agora && a.Status != "Cancelado")
                .AsQueryable();

            // Se não for admin, filtrar por funcionário
            if (!isAdmin)
            {
                query = query.Where(a => a.FuncionarioId == funcionarioId);
            }

            var agendamentos = await query
                .OrderBy(a => a.DataHora)
                .Select(a => new LembreteAgendamento
                {
                    Id = a.Id,
                    ClienteNome = a.Cliente != null ? a.Cliente.Nome : "",
                    FuncionarioNome = a.Funcionario != null ? a.Funcionario.Nome : "",
                    DataHora = a.DataHora,
                    Status = a.Status,
                    ConferenciaUrl = a.ConferenciaUrl,
                    TipoAgendamento = a.TipoAgendamento != null ? a.TipoAgendamento.Nome : ""
                })
                .ToListAsync();

            var viewModel = new DashboardLembreteViewModel
            {
                AgendamentosHoje = agendamentos
                    .Where(a => a.DataHora.Date == hoje)
                    .ToList(),
                
                Agendamentos3Dias = agendamentos
                    .Where(a => a.DataHora.Date > hoje && a.DataHora.Date <= em3Dias)
                    .ToList(),
                
                Agendamentos5Dias = agendamentos
                    .Where(a => a.DataHora.Date > em3Dias && a.DataHora.Date <= em5Dias)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}
