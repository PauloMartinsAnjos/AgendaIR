using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models
{
    /// <summary>
    /// Representa um participante adicional de um agendamento
    /// Ex: Reunião com casal - marido é cliente, esposa é participante
    /// </summary>
    public class AgendamentoParticipante
    {
        public int Id { get; set; }
        
        /// <summary>
        /// ID do agendamento pai
        /// </summary>
        public int AgendamentoId { get; set; }
        
        /// <summary>
        /// Email do participante (obrigatório para enviar convite)
        /// </summary>
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200, ErrorMessage = "Email não pode ter mais de 200 caracteres")]
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome do participante (opcional)
        /// </summary>
        [StringLength(200, ErrorMessage = "Nome não pode ter mais de 200 caracteres")]
        public string? Nome { get; set; }
        
        /// <summary>
        /// Data de criação do registro (UTC)
        /// </summary>
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        
        // ===== NAVIGATION PROPERTIES =====
        
        /// <summary>
        /// Agendamento pai
        /// </summary>
        public Agendamento Agendamento { get; set; } = null!;
    }
}
