using System.ComponentModel.DataAnnotations;

namespace AgendaIR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para login de funcionários
    /// Um ViewModel é um modelo específico para a View (tela)
    /// Ele contém apenas os dados necessários para aquela tela
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O username é obrigatório")]
        [Display(Name = "Usuário")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; set; }
    }
}
