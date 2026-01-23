using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel usado para criar novos agendamentos
    /// Funciona tanto para CLIENTES quanto para FUNCIONÁRIOS
    /// </summary>
    public class AgendamentoCreateViewModel
    {
        // === Campos para CLIENTE ===
        [Required(ErrorMessage = "A data e hora são obrigatórias")]
        [Display(Name = "Data e Hora do Agendamento")]
        public DateTime DataHora { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "Observações (Opcional)")]
        public string? Observacoes { get; set; }
        
        // Informações do funcionário atribuído (readonly para o cliente)
        public int FuncionarioId { get; set; }
        public string FuncionarioNome { get; set; } = string.Empty;
        
        // Lista de documentos que podem ser enviados (para cliente)
        // Preenchido pelo controller com todos os DocumentosSolicitados ativos
        public List<DocumentoUploadViewModel> Documentos { get; set; } = new List<DocumentoUploadViewModel>();

        // === Campos adicionais para FUNCIONÁRIO ===
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Display(Name = "Tipo de Agendamento")]
        public int TipoAgendamentoId { get; set; }
    }
}
