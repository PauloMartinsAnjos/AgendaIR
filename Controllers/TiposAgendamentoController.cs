using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;

namespace AgendaIR.Controllers
{
    public class TiposAgendamentoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TiposAgendamentoController> _logger;

        public TiposAgendamentoController(
            ApplicationDbContext context,
            ILogger<TiposAgendamentoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper: Verificar se é Admin
        private bool IsUserAdmin()
        {
            return User.FindFirst("IsAdmin")?.Value == "True";
        }

        // GET: TiposAgendamento
        public async Task<IActionResult> Index()
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var tipos = await _context.TiposAgendamento
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            return View(tipos);
        }

        // GET: TiposAgendamento/Create
        public IActionResult Create()
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            return View();
        }

        // POST: TiposAgendamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoAgendamento tipo)
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe tipo com mesmo nome
                var nomeExiste = await _context.TiposAgendamento
                    .AnyAsync(t => t.Nome.ToLower() == tipo.Nome.ToLower());

                if (nomeExiste)
                {
                    ModelState.AddModelError("Nome", "Já existe um tipo de agendamento com este nome");
                    return View(tipo);
                }

                tipo.DataCriacao = DateTime.UtcNow;
                tipo.Ativo = true;

                _context.Add(tipo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Tipo de agendamento '{tipo.Nome}' criado por {User.Identity?.Name}");

                TempData["Success"] = "Tipo de agendamento criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            return View(tipo);
        }

        // GET: TiposAgendamento/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var tipo = await _context.TiposAgendamento.FindAsync(id);

            if (tipo == null)
            {
                return NotFound();
            }

            return View(tipo);
        }

        // POST: TiposAgendamento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoAgendamento tipo)
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id != tipo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe outro tipo com mesmo nome
                var nomeExiste = await _context.TiposAgendamento
                    .AnyAsync(t => t.Nome.ToLower() == tipo.Nome.ToLower() && t.Id != id);

                if (nomeExiste)
                {
                    ModelState.AddModelError("Nome", "Já existe outro tipo de agendamento com este nome");
                    return View(tipo);
                }

                try
                {
                    var tipoOriginal = await _context.TiposAgendamento.FindAsync(id);
                    
                    if (tipoOriginal == null)
                    {
                        return NotFound();
                    }

                    tipoOriginal.Nome = tipo.Nome;
                    tipoOriginal.Descricao = tipo.Descricao;
                    tipoOriginal.Ativo = tipo.Ativo;

                    _context.Update(tipoOriginal);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Tipo de agendamento {id} atualizado por {User.Identity?.Name}");

                    TempData["Success"] = "Tipo de agendamento atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipoAgendamentoExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(tipo);
        }

        // POST: TiposAgendamento/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsUserAdmin())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var tipo = await _context.TiposAgendamento.FindAsync(id);

            if (tipo == null)
            {
                return NotFound();
            }

            // Soft delete: marcar como inativo
            tipo.Ativo = false;
            _context.Update(tipo);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tipo de agendamento {id} desativado por {User.Identity?.Name}");

            TempData["Success"] = "Tipo de agendamento desativado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API: GET /api/tiposagendamento/{id}/documentos
        /// Retorna documentos associados a um tipo de agendamento
        /// </summary>
        [HttpGet]
        [Route("api/tiposagendamento/{id}/documentos")]
        public async Task<IActionResult> GetDocumentosPorTipo(int id)
        {
            var tipo = await _context.TiposAgendamento
                .Include(t => t.DocumentosSolicitados)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null)
            {
                return NotFound(new { erro = "Tipo de agendamento não encontrado" });
            }

            var documentos = tipo.DocumentosSolicitados
                .Where(d => d.Ativo)
                .OrderByDescending(d => d.Obrigatorio)
                .ThenBy(d => d.Nome)
                .Select(d => new
                {
                    id = d.Id,
                    nome = d.Nome,
                    descricao = d.Descricao,
                    obrigatorio = d.Obrigatorio
                })
                .ToList();

            return Ok(documentos);
        }

        private bool TipoAgendamentoExists(int id)
        {
            return _context.TiposAgendamento.Any(e => e.Id == id);
        }
    }
}
