using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para a listagem de agendamentos com filtros
    /// Usado tanto por funcionários (seus agendamentos) quanto admins (todos)
    /// </summary>
    public class AgendamentoIndexViewModel
    {
        // ===== Filtros =====
        public int? FiltroFuncionarioId { get; set; }
        public int? FiltroClienteId { get; set; }
        public int? FiltroTipoId { get; set; }
        public string? FiltroStatus { get; set; }
        public DateTime? FiltroDataInicio { get; set; }
        public DateTime? FiltroDataFim { get; set; }

        // ===== Dados =====
        public List<AgendamentoListItem> Agendamentos { get; set; } = new();
        
        // ===== Dropdowns =====
        public List<FuncionarioSelectItem> Funcionarios { get; set; } = new();
        public List<ClienteSelectItem> Clientes { get; set; } = new();
        public List<TipoAgendamentoSelectItem> TiposAgendamento { get; set; } = new();
        
        // ===== Controle =====
        public bool IsAdmin { get; set; }
        public int FuncionarioLogadoId { get; set; }
        public string FuncionarioLogadoNome { get; set; } = string.Empty;
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
        public string? ClienteCPF { get; set; }
        public string ClienteEmail { get; set; } = string.Empty;
        public string FuncionarioNome { get; set; } = string.Empty;
        public string? TipoAgendamentoNome { get; set; }
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

    /// <summary>
    /// Item para dropdown de seleção de cliente
    /// </summary>
    public class ClienteSelectItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    /// <summary>
    /// Item para dropdown de seleção de tipo de agendamento
    /// </summary>
    public class TipoAgendamentoSelectItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
