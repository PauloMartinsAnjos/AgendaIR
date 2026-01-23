using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AgendaIR.Data;
using AgendaIR.Models;
using AgendaIR.Models.ViewModels;

namespace AgendaIR.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de documentos solicitados (CRUD completo)
    /// Documentos solicitados são GLOBAIS - todos os clientes veem os mesmos documentos
    /// Acessível por Funcionários e Administradores
    /// Permite criar, editar, ativar/desativar e deletar tipos de documentos
    /// </summary>
    [Authorize] // Requer que o usuário esteja autenticado
    public class DocumentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentosController> _logger;

        public DocumentosController(
            ApplicationDbContext context,
            ILogger<DocumentosController> logger)
        {
            _context = context;
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
        /// Verifica se o usuário tem permissão para acessar documentos
        /// Apenas funcionários e administradores podem acessar
        /// </summary>
        private bool HasPermission()
        {
            return IsUserFuncionario() || IsUserAdmin();
        }

        // GET: Documentos
        /// <summary>
        /// Lista todos os documentos solicitados (lista GLOBAL)
        /// Exibe documentos ativos e inativos com badges de status
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Verificar permissão
            if (!HasPermission())
            {
                _logger.LogWarning($"Usuário {User.Identity?.Name} tentou acessar documentos sem permissão");
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Buscar todos os documentos ordenados por nome
            var documentos = await _context.DocumentosSolicitados
                .OrderBy(d => d.Nome)
                .ToListAsync();

            return View(documentos);
        }

        // GET: Documentos/Create
        /// <summary>
        /// Exibe formulário para criar novo documento solicitado
        /// </summary>
        public async Task<IActionResult> Create()
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Carregar tipos ativos para dropdown
            ViewBag.TiposAgendamento = await _context.TiposAgendamento
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();

            var model = new DocumentoSolicitadoViewModel
            {
                Ativo = true // Por padrão, novos documentos são criados como ativos
            };

            return View(model);
        }

        // POST: Documentos/Create
        /// <summary>
        /// Processa criação de novo documento solicitado
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentoSolicitadoViewModel model)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe documento com mesmo nome
                var nomeExistente = await _context.DocumentosSolicitados
                    .AnyAsync(d => d.Nome.ToLower() == model.Nome.ToLower());

                if (nomeExistente)
                {
                    ModelState.AddModelError("Nome", "Já existe um documento com este nome");
                    ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                    return View(model);
                }

                // Criar novo documento
                var documento = new DocumentoSolicitado
                {
                    Nome = model.Nome,
                    Descricao = model.Descricao,
                    Obrigatorio = model.Obrigatorio,
                    TipoAgendamentoId = model.TipoAgendamentoId,
                    Ativo = model.Ativo,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Add(documento);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Novo documento solicitado criado: {documento.Nome} (ID: {documento.Id}) por {User.Identity?.Name}");

                TempData["Success"] = "Documento criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
            return View(model);
        }

        // GET: Documentos/Edit/5
        /// <summary>
        /// Exibe formulário para editar documento solicitado
        /// Permite editar todos os campos incluindo status Ativo e Obrigatório
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var documento = await _context.DocumentosSolicitados.FindAsync(id);

            if (documento == null)
            {
                return NotFound();
            }

            // Mapear para ViewModel
            var model = new DocumentoSolicitadoViewModel
            {
                Id = documento.Id,
                Nome = documento.Nome,
                Descricao = documento.Descricao,
                Obrigatorio = documento.Obrigatorio,
                TipoAgendamentoId = documento.TipoAgendamentoId,
                Ativo = documento.Ativo,
                DataCriacao = documento.DataCriacao
            };

            // Carregar tipos ativos para dropdown
            ViewBag.TiposAgendamento = await _context.TiposAgendamento
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();

            return View(model);
        }

        // POST: Documentos/Edit/5
        /// <summary>
        /// Processa edição de documento solicitado
        /// Permite alterar todos os campos incluindo toggles de Ativo e Obrigatório
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentoSolicitadoViewModel model)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe outro documento com mesmo nome
                var nomeExistente = await _context.DocumentosSolicitados
                    .AnyAsync(d => d.Nome.ToLower() == model.Nome.ToLower() && d.Id != id);

                if (nomeExistente)
                {
                    ModelState.AddModelError("Nome", "Já existe outro documento com este nome");
                    ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
                    return View(model);
                }

                try
                {
                    // Buscar documento original
                    var documento = await _context.DocumentosSolicitados.FindAsync(id);
                    
                    if (documento == null)
                    {
                        return NotFound();
                    }

                    // Atualizar campos
                    documento.Nome = model.Nome;
                    documento.Descricao = model.Descricao;
                    documento.Obrigatorio = model.Obrigatorio;
                    documento.TipoAgendamentoId = model.TipoAgendamentoId;
                    documento.Ativo = model.Ativo;
                    // DataCriacao não é alterada

                    _context.Update(documento);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Documento {documento.Id} atualizado por {User.Identity?.Name}");

                    TempData["Success"] = "Documento atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentoSolicitadoExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.TiposAgendamento = await _context.TiposAgendamento.Where(t => t.Ativo).ToListAsync();
            return View(model);
        }

        // GET: Documentos/Delete/5
        /// <summary>
        /// Exibe confirmação para deletar documento
        /// Avisa se existem documentos anexados relacionados
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var documento = await _context.DocumentosSolicitados
                .Include(d => d.DocumentosAnexados)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (documento == null)
            {
                return NotFound();
            }

            // Mapear para ViewModel incluindo contagem de documentos anexados
            var model = new DocumentoSolicitadoViewModel
            {
                Id = documento.Id,
                Nome = documento.Nome,
                Descricao = documento.Descricao,
                Obrigatorio = documento.Obrigatorio,
                Ativo = documento.Ativo,
                DataCriacao = documento.DataCriacao,
                QuantidadeDocumentosAnexados = documento.DocumentosAnexados?.Count ?? 0
            };

            return View(model);
        }

        // POST: Documentos/Delete/5
        /// <summary>
        /// Confirma e executa deleção do documento
        /// Só permite deletar se não houver documentos anexados relacionados
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var documento = await _context.DocumentosSolicitados
                .Include(d => d.DocumentosAnexados)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (documento == null)
            {
                return NotFound();
            }

            // Verificar se existem documentos anexados
            if (documento.DocumentosAnexados != null && documento.DocumentosAnexados.Any())
            {
                TempData["Error"] = $"Não é possível deletar este documento pois existem {documento.DocumentosAnexados.Count} documento(s) anexado(s) relacionado(s). Considere desativar o documento ao invés de deletá-lo.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            _context.DocumentosSolicitados.Remove(documento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Documento {id} deletado por {User.Identity?.Name}");

            TempData["Success"] = "Documento deletado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Documentos/ToggleAtivo/5
        /// <summary>
        /// Alterna o status Ativo de um documento sem deletá-lo
        /// Permite desativar documentos mantendo o histórico no banco
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            // Verificar permissão
            if (!HasPermission())
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var documento = await _context.DocumentosSolicitados.FindAsync(id);

            if (documento == null)
            {
                return NotFound();
            }

            // Alternar status
            documento.Ativo = !documento.Ativo;
            
            _context.Update(documento);
            await _context.SaveChangesAsync();

            var statusTexto = documento.Ativo ? "ativado" : "desativado";
            _logger.LogInformation($"Documento {documento.Nome} (ID: {id}) {statusTexto} por {User.Identity?.Name}");

            TempData["Success"] = $"Documento '{documento.Nome}' foi {statusTexto} com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Verifica se documento existe
        /// </summary>
        private bool DocumentoSolicitadoExists(int id)
        {
            return _context.DocumentosSolicitados.Any(e => e.Id == id);
        }

        /// <summary>
        /// API: Retorna documentos de um tipo específico (para AJAX)
        /// </summary>
        [HttpGet("/api/documentos/porTipo/{tipoId}")]
        public async Task<IActionResult> GetDocumentosPorTipo(int tipoId)
        {
            var documentos = await _context.DocumentosSolicitados
                .Where(d => d.TipoAgendamentoId == tipoId && d.Ativo)
                .OrderByDescending(d => d.Obrigatorio)
                .ThenBy(d => d.Nome)
                .Select(d => new 
                { 
                    id = d.Id, 
                    nome = d.Nome,
                    descricao = d.Descricao,
                    obrigatorio = d.Obrigatorio 
                })
                .ToListAsync();

            return Json(documentos);
        }
    }
}
