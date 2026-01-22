using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaIR.Models
{
    /// <summary>
    /// Modelo que representa um agendamento de atendimento IR
    /// Conecta um cliente a um funcionário em uma data/hora específica
    /// </summary>
    public class Agendamento
    {
        // Id é a chave primária
        [Key]
        public int Id { get; set; }

        // ClienteId é a chave estrangeira para o cliente
        [Required]
        public int ClienteId { get; set; }

        // FuncionarioId é a chave estrangeira para o funcionário
        [Required]
        public int FuncionarioId { get; set; }

        // Data e hora do agendamento
        [Required(ErrorMessage = "A data e hora são obrigatórias")]
        public DateTime DataHora { get; set; }

        // Status do agendamento
        // Valores possíveis: Pendente, Confirmado, Concluído, Cancelado
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pendente";

        // ID do evento criado no Google Calendar
        // Usado para atualizar ou deletar o evento quando necessário
        [StringLength(500)]
        public string? GoogleCalendarEventId { get; set; }

        // Observações sobre o agendamento
        [StringLength(1000)]
        public string? Observacoes { get; set; }

        // Data de criação do agendamento
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Data da última atualização
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

        // Propriedade de navegação para o Cliente
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        // Propriedade de navegação para o Funcionário
        [ForeignKey("FuncionarioId")]
        public virtual Funcionario? Funcionario { get; set; }

        // Relacionamento: Um agendamento pode ter vários documentos anexados
        public virtual ICollection<DocumentoAnexado> DocumentosAnexados { get; set; } = new List<DocumentoAnexado>();
    }

    /// <summary>
    /// Enum para os possíveis status de um agendamento
    /// Usar enum garante que apenas valores válidos sejam usados
    /// </summary>
    public enum StatusAgendamento
    {
        Pendente,
        Confirmado,
        Concluido,
        Cancelado
    }
}
