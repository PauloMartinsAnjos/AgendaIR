using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel usado quando um FUNCIONÁRIO ou ADMIN está editando um agendamento
    /// Permite alterar status, data/hora e adicionar observações
    /// </summary>
    public class AgendamentoEditViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pendente";
        
        [StringLength(1000)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }
        
        [Required]
        [Display(Name = "Data e Hora")]
        public DateTime DataHora { get; set; }
        
        // Campos de controle para Google Calendar
        public DateTime DataHoraOriginal { get; set; }
        public string? GoogleCalendarEventId { get; set; }
        public string? FuncionarioGoogleEmail { get; set; }
        
        // Informações do cliente (readonly)
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string ClienteTelefone { get; set; } = string.Empty;
        
        // Informações do funcionário (readonly)
        public string FuncionarioNome { get; set; } = string.Empty;
        
        // Documentos anexados (readonly)
        public List<DocumentoAnexado> DocumentosAnexados { get; set; } = new List<DocumentoAnexado>();
        
        // Lista de status possíveis para dropdown
        public List<string> StatusList { get; set; } = new List<string> 
        { 
            "Pendente", 
            "Confirmado", 
            "Concluido", 
            "Cancelado" 
        };
    }
}
