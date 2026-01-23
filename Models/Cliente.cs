using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaIR.Models
{
    /// <summary>
    /// Modelo que representa um cliente do sistema
    /// Clientes fazem login através de um link mágico (magic link)
    /// Cada cliente está vinculado a um funcionário específico
    /// </summary>
    public class Cliente
    {
        // Id é a chave primária (Primary Key)
        [Key]
        public int Id { get; set; }

        // Nome completo do cliente
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        // Email do cliente
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        // Telefone/WhatsApp do cliente
        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(20)]
        public string Telefone { get; set; } = string.Empty;

        // Telefone residencial do cliente
        [StringLength(20)]
        [Display(Name = "Telefone Residencial")]
        public string? TelefoneResidencial { get; set; }

        // Telefone comercial do cliente
        [StringLength(20)]
        [Display(Name = "Telefone Comercial")]
        public string? TelefoneComercial { get; set; }

        // Observações sobre o cliente
        [Display(Name = "Observações")]
        [StringLength(2000, ErrorMessage = "As observações não podem exceder 2000 caracteres")]
        public string? Observacoes { get; set; }

        // Cor da pasta física no escritório
        [StringLength(20)]
        [Display(Name = "Cor da Pasta")]
        public string? CorDaPasta { get; set; }

        // CPF do cliente
        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)]
        public string CPF { get; set; } = string.Empty;

        // FuncionarioId é a chave estrangeira (Foreign Key) que vincula o cliente a um funcionário
        // Este vínculo é IMUTÁVEL - uma vez definido, não pode ser alterado
        [Required]
        public int FuncionarioId { get; set; }

        // MagicToken é o token único que permite login automático
        // É como uma "senha mágica" que dá acesso ao sistema
        [Required]
        [StringLength(500)]
        public string MagicToken { get; set; } = string.Empty;

        // Data e hora em que o token foi gerado
        public DateTime TokenGeradoEm { get; set; } = DateTime.UtcNow;

        // Indica se o cliente está ativo
        public bool Ativo { get; set; } = true;

        // Data de criação do registro
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Propriedade de navegação: referência ao funcionário responsável
        // O Entity Framework usa isso para fazer JOIN entre as tabelas
        [ForeignKey("FuncionarioId")]
        public virtual Funcionario? Funcionario { get; set; }

        // Relacionamento: Um cliente pode ter vários agendamentos
        public virtual ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
