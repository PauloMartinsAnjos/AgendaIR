using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para edição de cliente existente
    /// FuncionarioId não pode ser alterado após criação
    /// </summary>
    public class ClienteEditViewModel
    {
        /// <summary>
        /// Id do cliente (hidden field)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome completo do cliente
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome não pode ter mais que 200 caracteres")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Email do cliente
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Telefone/WhatsApp do cliente
        /// </summary>
        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(20)]
        [Display(Name = "Telefone/WhatsApp")]
        [Phone(ErrorMessage = "Telefone inválido")]
        public string Telefone { get; set; } = string.Empty;

        /// <summary>
        /// Telefone residencial do cliente
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Telefone Residencial")]
        public string? TelefoneResidencial { get; set; }

        /// <summary>
        /// Telefone comercial do cliente
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Telefone Comercial")]
        public string? TelefoneComercial { get; set; }

        /// <summary>
        /// Observações sobre o cliente
        /// </summary>
        [Display(Name = "Observações")]
        [StringLength(2000, ErrorMessage = "As observações não podem exceder 2000 caracteres")]
        public string? Observacoes { get; set; }

        /// <summary>
        /// Cor da pasta física no escritório
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Cor da Pasta")]
        public string? CorDaPasta { get; set; }

        /// <summary>
        /// CPF do cliente
        /// </summary>
        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)]
        [Display(Name = "CPF")]
        public string CPF { get; set; } = string.Empty;

        /// <summary>
        /// Indica se o cliente está ativo
        /// </summary>
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// FuncionarioId - APENAS PARA EXIBIÇÃO (readonly)
        /// Este campo é IMUTÁVEL e não pode ser alterado
        /// </summary>
        [Display(Name = "Funcionário Responsável")]
        public int FuncionarioId { get; set; }

        /// <summary>
        /// Nome do funcionário responsável (para exibição)
        /// </summary>
        [Display(Name = "Funcionário Responsável")]
        public string? FuncionarioNome { get; set; }
    }
}
