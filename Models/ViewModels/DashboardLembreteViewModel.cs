using System;
using System.Collections.Generic;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para exibir lembretes de agendamentos próximos no dashboard
    /// </summary>
    public class DashboardLembreteViewModel
    {
        /// <summary>
        /// Agendamentos para hoje
        /// </summary>
        public List<LembreteAgendamento> AgendamentosHoje { get; set; } = new List<LembreteAgendamento>();

        /// <summary>
        /// Agendamentos nos próximos 3 dias
        /// </summary>
        public List<LembreteAgendamento> Agendamentos3Dias { get; set; } = new List<LembreteAgendamento>();

        /// <summary>
        /// Agendamentos nos próximos 5 dias
        /// </summary>
        public List<LembreteAgendamento> Agendamentos5Dias { get; set; } = new List<LembreteAgendamento>();
    }

    /// <summary>
    /// Representa um agendamento resumido para exibição nos lembretes
    /// </summary>
    public class LembreteAgendamento
    {
        public int Id { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string FuncionarioNome { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ConferenciaUrl { get; set; }
        public string TipoAgendamento { get; set; } = string.Empty;
    }
}
