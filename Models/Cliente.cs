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

        /// <summary>
        /// Funcionário responsável principal pelo cliente
        /// </summary>
        [Required]
        public int FuncionarioResponsavelId { get; set; }

        /// <summary>
        /// Navegação para funcionário responsável
        /// </summary>
        public Funcionario? FuncionarioResponsavel { get; set; }

        // MagicToken é o token único que permite login automático
        // É como uma "senha mágica" que dá acesso ao sistema
        // Campos permanecem no banco mas não são preenchidos automaticamente na criação
        [StringLength(500)]
        public string? MagicToken { get; set; }

        // Data e hora em que o token foi gerado
        public DateTime? TokenGeradoEm { get; set; }

        /// <summary>
        /// Data/hora de expiração do token (UTC)
        /// </summary>
        public DateTime? TokenExpiracao { get; set; }

        /// <summary>
        /// Indica se o token está ativo (não expirou ou foi revogado)
        /// </summary>
        public bool TokenAtivo { get; set; } = false;

        // Indica se o cliente está ativo
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Verifica se o token está válido (não expirou)
        /// </summary>
        public bool TokenValido()
        {
            if (!TokenAtivo || !TokenExpiracao.HasValue)
                return false;
            
            return DateTime.UtcNow < TokenExpiracao.Value;
        }

        // Data de criação do registro
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relacionamento: Um cliente pode ter vários agendamentos
        public virtual ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
