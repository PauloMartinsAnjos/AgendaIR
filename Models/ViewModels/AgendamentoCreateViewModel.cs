using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel usado quando um CLIENTE está criando um novo agendamento
    /// Contém todos os campos necessários para criar o agendamento + upload de documentos
    /// </summary>
    public class AgendamentoCreateViewModel
    {
        [Required(ErrorMessage = "A data e hora são obrigatórias")]
        [Display(Name = "Data e Hora do Agendamento")]
        public DateTime DataHora { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "Observações (Opcional)")]
        public string? Observacoes { get; set; }
        
        // Informações do funcionário atribuído (readonly para o cliente)
        public int FuncionarioId { get; set; }
        public string FuncionarioNome { get; set; } = string.Empty;
        
        // Lista de documentos que podem ser enviados
        // Preenchido pelo controller com todos os DocumentosSolicitados ativos
        public List<DocumentoUploadViewModel> Documentos { get; set; } = new List<DocumentoUploadViewModel>();
    }
}
