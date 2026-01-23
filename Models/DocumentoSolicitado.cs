using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models
{
    /// <summary>
    /// Modelo que representa um tipo de documento que pode ser solicitado aos clientes
    /// Esta é uma lista GLOBAL - todos os clientes veem os mesmos documentos
    /// Exemplo: RG, CPF, Comprovante de Residência, etc.
    /// </summary>
    public class DocumentoSolicitado
    {
        // Id é a chave primária
        [Key]
        public int Id { get; set; }

        // Nome do documento
        // Exemplo: "RG", "Comprovante de Residência"
        [Required(ErrorMessage = "O nome do documento é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        // Descrição/Instruções sobre o documento
        // Exemplo: "Envie frente e verso em um único arquivo PDF"
        [StringLength(500)]
        public string? Descricao { get; set; }

        // Indica se este documento é obrigatório para todos os agendamentos
        // Se true, o cliente DEVE anexar este documento ao fazer um agendamento
        public bool Obrigatorio { get; set; }

        /// <summary>
        /// Tipo de agendamento relacionado (nullable - doc pode ser genérico)
        /// </summary>
        [Display(Name = "Tipo de Agendamento")]
        public int? TipoAgendamentoId { get; set; }

        /// <summary>
        /// Relacionamento com tipo de agendamento
        /// </summary>
        public TipoAgendamento? TipoAgendamento { get; set; }

        // Indica se o documento está ativo
        // Permite desativar documentos sem deletá-los do banco
        public bool Ativo { get; set; } = true;

        // Data de criação do registro
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relacionamento: Um tipo de documento pode ter vários arquivos anexados
        public virtual ICollection<DocumentoAnexado> DocumentosAnexados { get; set; } = new List<DocumentoAnexado>();
    }
}
