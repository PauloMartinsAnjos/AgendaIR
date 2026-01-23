using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para a dashboard principal do sistema
    /// Contém KPIs, estatísticas e dados para gráficos
    /// </summary>
    public class DashboardViewModel
    {
        // ===== KPIs =====
        /// <summary>
        /// Total de agendamentos no sistema
        /// </summary>
        public int TotalAgendamentos { get; set; }

        /// <summary>
        /// Total de clientes ativos
        /// </summary>
        public int TotalClientes { get; set; }

        /// <summary>
        /// Total de funcionários ativos
        /// </summary>
        public int TotalFuncionarios { get; set; }

        /// <summary>
        /// Taxa de conclusão dos agendamentos (%)
        /// </summary>
        public decimal TaxaConclusao { get; set; }

        // ===== Agendamentos por Status =====
        /// <summary>
        /// Total de agendamentos concluídos
        /// </summary>
        public int AgendamentosConcluidos { get; set; }

        /// <summary>
        /// Total de agendamentos pendentes
        /// </summary>
        public int AgendamentosPendentes { get; set; }

        /// <summary>
        /// Total de agendamentos cancelados
        /// </summary>
        public int AgendamentosCancelados { get; set; }

        /// <summary>
        /// Total de agendamentos confirmados
        /// </summary>
        public int AgendamentosConfirmados { get; set; }

        // ===== Top Status =====
        /// <summary>
        /// Lista com os status mais comuns
        /// </summary>
        public List<StatusCount> TopStatus { get; set; } = new List<StatusCount>();

        // ===== Tendência Semanal =====
        /// <summary>
        /// Dados de agendamentos dos últimos 7 dias
        /// </summary>
        public List<AgendamentoDia> UltimosSeteDias { get; set; } = new List<AgendamentoDia>();
    }

    /// <summary>
    /// Representa um status de agendamento com sua quantidade
    /// </summary>
    public class StatusCount
    {
        /// <summary>
        /// Nome do status
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de agendamentos com este status
        /// </summary>
        public int Quantidade { get; set; }
    }

    /// <summary>
    /// Representa a quantidade de agendamentos em um dia específico
    /// </summary>
    public class AgendamentoDia
    {
        /// <summary>
        /// Dia no formato dd/MM
        /// </summary>
        public string Dia { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de agendamentos criados neste dia
        /// </summary>
        public int Quantidade { get; set; }
    }
}
