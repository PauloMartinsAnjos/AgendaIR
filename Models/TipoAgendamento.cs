using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models
{
    /// <summary>
    /// Representa um tipo de agendamento (ex: Declaração IR, Abertura de MEI)
    /// Admin pode criar tipos personalizados
    /// </summary>
    public class TipoAgendamento
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais que 100 caracteres")]
        [Display(Name = "Nome do Tipo")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição não pode ter mais que 500 caracteres")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Local do atendimento (ex: "Escritório", "Online", "Casa do Cliente")
        /// </summary>
        [StringLength(200)]
        [Display(Name = "Local")]
        public string? Local { get; set; }

        /// <summary>
        /// Se true, cria link do Google Meet automaticamente
        /// </summary>
        [Display(Name = "Criar Google Meet")]
        public bool CriarGoogleMeet { get; set; } = false;

        /// <summary>
        /// Cor do evento no Google Calendar (1-11)
        /// 6 = Laranja (padrão RIR)
        /// </summary>
        [Display(Name = "Cor no Calendário")]
        public int CorCalendario { get; set; } = 6;

        /// <summary>
        /// Se true, marca horário como ocupado no calendário
        /// Se false, marca como disponível (transparente)
        /// </summary>
        [Display(Name = "Bloqueia Horário")]
        public bool BloqueiaHorario { get; set; } = true;

        /// <summary>
        /// JSON com lista de documentos obrigatórios para este tipo
        /// Exemplo: ["RG e CPF", "Comprovante Renda", "Recibos Médicos"]
        /// </summary>
        [Display(Name = "Documentos Obrigatórios (JSON)")]
        public string? DocumentosObrigatoriosJson { get; set; }

        /// <summary>
        /// Relacionamento: Um tipo pode ter vários documentos solicitados
        /// </summary>
        public virtual ICollection<DocumentoSolicitado> DocumentosSolicitados { get; set; } = new List<DocumentoSolicitado>();

        /// <summary>
        /// Relacionamento: Um tipo pode ter vários agendamentos
        /// </summary>
        public virtual ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
