using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para a listagem de agendamentos com filtros
    /// Usado tanto por funcionários (seus agendamentos) quanto admins (todos)
    /// </summary>
    public class AgendamentoIndexViewModel
    {
        // Filtros
        [Display(Name = "Status")]
        public string? FiltroStatus { get; set; }
        
        [Display(Name = "Data Início")]
        [DataType(DataType.Date)]
        public DateTime? FiltroDataInicio { get; set; }
        
        [Display(Name = "Data Fim")]
        [DataType(DataType.Date)]
        public DateTime? FiltroDataFim { get; set; }
        
        [Display(Name = "Funcionário")]
        public int? FiltroFuncionarioId { get; set; }
        
        // Lista de agendamentos filtrados
        public List<AgendamentoListItem> Agendamentos { get; set; } = new List<AgendamentoListItem>();
        
        // Lista de funcionários para o filtro (apenas para admins)
        public List<FuncionarioSelectItem>? Funcionarios { get; set; }
        
        // Lista de status para o filtro
        public List<string> StatusList { get; set; } = new List<string> 
        { 
            "Pendente", 
            "Confirmado", 
            "Concluido", 
            "Cancelado" 
        };
    }
    
    /// <summary>
    /// Representa um item individual na lista de agendamentos
    /// </summary>
    public class AgendamentoListItem
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string FuncionarioNome { get; set; } = string.Empty;
        public int TotalDocumentos { get; set; }
        public DateTime DataCriacao { get; set; }
    }
    
    /// <summary>
    /// Item para dropdown de seleção de funcionário
    /// </summary>
    public class FuncionarioSelectItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
