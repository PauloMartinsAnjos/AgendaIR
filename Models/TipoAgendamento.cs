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
        /// Relacionamento: Um tipo pode ter vários documentos solicitados
        /// </summary>
        public virtual ICollection<DocumentoSolicitado> DocumentosSolicitados { get; set; } = new List<DocumentoSolicitado>();

        /// <summary>
        /// Relacionamento: Um tipo pode ter vários agendamentos
        /// </summary>
        public virtual ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
