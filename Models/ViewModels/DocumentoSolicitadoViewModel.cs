using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para criação e edição de documentos solicitados
    /// Usado nos formulários de gerenciamento de documentos
    /// </summary>
    public class DocumentoSolicitadoViewModel
    {
        /// <summary>
        /// Id do documento (apenas para edição)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do documento solicitado
        /// Exemplo: "RG", "CPF", "Comprovante de Residência"
        /// </summary>
        [Required(ErrorMessage = "O nome do documento é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais que 100 caracteres")]
        [Display(Name = "Nome do Documento")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Descrição ou instruções sobre o documento
        /// Exemplo: "Envie frente e verso em um único arquivo PDF"
        /// </summary>
        [StringLength(500, ErrorMessage = "A descrição não pode ter mais que 500 caracteres")]
        [Display(Name = "Descrição/Instruções")]
        public string? Descricao { get; set; }

        /// <summary>
        /// Indica se o documento é obrigatório
        /// </summary>
        [Display(Name = "Obrigatório")]
        public bool Obrigatorio { get; set; }

        /// <summary>
        /// Tipo de agendamento relacionado (opcional)
        /// </summary>
        [Display(Name = "Tipo de Agendamento")]
        public int? TipoAgendamentoId { get; set; }

        /// <summary>
        /// Indica se o documento está ativo
        /// </summary>
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Data de criação (apenas para exibição)
        /// </summary>
        [Display(Name = "Data de Criação")]
        public DateTime? DataCriacao { get; set; }

        /// <summary>
        /// Quantidade de documentos anexados (apenas para exibição no delete)
        /// </summary>
        public int QuantidadeDocumentosAnexados { get; set; }
    }
}
