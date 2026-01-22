using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaIR.Models
{
    /// <summary>
    /// Modelo que representa um arquivo de documento anexado a um agendamento
    /// Cada arquivo está vinculado a um tipo de documento (DocumentoSolicitado)
    /// e a um agendamento específico
    /// </summary>
    public class DocumentoAnexado
    {
        // Id é a chave primária
        [Key]
        public int Id { get; set; }

        // AgendamentoId é a chave estrangeira para o agendamento
        [Required]
        public int AgendamentoId { get; set; }

        // DocumentoSolicitadoId indica qual tipo de documento é este
        [Required]
        public int DocumentoSolicitadoId { get; set; }

        // Nome original do arquivo enviado pelo cliente
        [Required]
        [StringLength(255)]
        public string NomeArquivo { get; set; } = string.Empty;

        // Caminho onde o arquivo foi salvo no servidor
        // Exemplo: /uploads/2024/01/15/abc123.pdf
        [Required]
        [StringLength(500)]
        public string CaminhoArquivo { get; set; } = string.Empty;

        // Tamanho do arquivo em bytes
        public long TamanhoBytes { get; set; }

        // Data e hora do upload
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;

        // Propriedade de navegação para o Agendamento
        [ForeignKey("AgendamentoId")]
        public virtual Agendamento? Agendamento { get; set; }

        // Propriedade de navegação para o DocumentoSolicitado
        [ForeignKey("DocumentoSolicitadoId")]
        public virtual DocumentoSolicitado? DocumentoSolicitado { get; set; }
    }
}
