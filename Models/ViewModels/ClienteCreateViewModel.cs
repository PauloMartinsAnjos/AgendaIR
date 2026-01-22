using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para criação de novo cliente
    /// Usado no formulário de cadastro de clientes
    /// </summary>
    public class ClienteCreateViewModel
    {
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
        /// CPF do cliente
        /// </summary>
        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)]
        [Display(Name = "CPF")]
        public string CPF { get; set; } = string.Empty;

        /// <summary>
        /// Id do funcionário responsável
        /// Se for funcionário comum, este valor será preenchido automaticamente
        /// Se for admin, pode selecionar qualquer funcionário
        /// </summary>
        [Required(ErrorMessage = "É necessário selecionar um funcionário")]
        [Display(Name = "Funcionário Responsável")]
        public int FuncionarioId { get; set; }
    }
}
