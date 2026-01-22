using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaIR.Models
{
    /// <summary>
    /// Modelo que representa um funcionário do sistema
    /// Funcionários podem fazer login com usuário e senha
    /// Eles são responsáveis por atender clientes e gerenciar agendamentos
    /// </summary>
    public class Funcionario
    {
        // Id é a chave primária (Primary Key) - identificador único do funcionário
        [Key]
        public int Id { get; set; }

        // Nome completo do funcionário
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome não pode ter mais que 200 caracteres")]
        public string Nome { get; set; } = string.Empty;

        // Email do funcionário
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        // Username é usado para fazer login no sistema
        [Required(ErrorMessage = "O username é obrigatório")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        // SenhaHash armazena a senha criptografada (nunca armazene senhas em texto puro!)
        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        // CPF do funcionário
        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)] // formato: 000.000.000-00
        public string CPF { get; set; } = string.Empty;

        // Email da conta Google Calendar do funcionário (para integração)
        [EmailAddress]
        [StringLength(200)]
        public string? GoogleCalendarEmail { get; set; }

        // Token de autenticação OAuth para Google Calendar
        // Este token permite que o sistema acesse o calendário do funcionário
        public string? GoogleCalendarToken { get; set; }

        // Indica se este funcionário é um administrador
        // Admins têm acesso total ao sistema
        public bool IsAdmin { get; set; }

        // Indica se o funcionário está ativo (pode desativar sem deletar)
        public bool Ativo { get; set; } = true;

        // Data de criação do registro
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relacionamento: Um funcionário pode ter vários clientes
        // Esta é uma navegação que o Entity Framework usa para relacionar as tabelas
        public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

        // Relacionamento: Um funcionário pode ter vários agendamentos
        public virtual ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
