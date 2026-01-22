using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para criação de um novo funcionário
    /// Separa a lógica de criação do modelo principal
    /// </summary>
    public class FuncionarioCreateViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome não pode ter mais que 200 caracteres")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O username é obrigatório")]
        [StringLength(100, ErrorMessage = "O username não pode ter mais que 100 caracteres")]
        [Display(Name = "Nome de Usuário")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação de senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("Senha", ErrorMessage = "A senha e a confirmação não correspondem")]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)]
        [Display(Name = "CPF")]
        [RegularExpression(@"\d{3}\.\d{3}\.\d{3}-\d{2}", ErrorMessage = "CPF deve estar no formato 000.000.000-00")]
        public string CPF { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email do Google Calendar inválido")]
        [StringLength(200)]
        [Display(Name = "E-mail do Google Calendar")]
        public string? GoogleCalendarEmail { get; set; }

        [Display(Name = "É Administrador?")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}
