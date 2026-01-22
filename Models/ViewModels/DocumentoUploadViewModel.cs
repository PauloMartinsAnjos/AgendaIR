using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para upload de documentos durante a criação do agendamento
    /// Representa um documento que pode ser anexado
    /// </summary>
    public class DocumentoUploadViewModel
    {
        public int DocumentoSolicitadoId { get; set; }
        
        public string Nome { get; set; } = string.Empty;
        
        public string? Descricao { get; set; }
        
        public bool Obrigatorio { get; set; }
        
        // O arquivo enviado pelo cliente
        public IFormFile? Arquivo { get; set; }
    }
}
