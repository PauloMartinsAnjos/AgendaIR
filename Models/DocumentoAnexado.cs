using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaIR.Models
{
    /// <summary>
    /// Representa um documento anexado a um agendamento
    /// </summary>
    [Table("DocumentosAnexados")]
    public class DocumentoAnexado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AgendamentoId { get; set; }

        [Required]
        public int DocumentoSolicitadoId { get; set; }

        [Required]
        [StringLength(255)]
        public string NomeArquivo { get; set; } = string.Empty;

        // ✅ NOVO: Conteúdo do arquivo comprimido (GZip)
        [Required]
        public byte[] ConteudoComprimido { get; set; } = Array.Empty<byte>();

        // ✅ NOVO: Tamanho original (antes da compressão)
        [Required]
        public long TamanhoOriginalBytes { get; set; }

        // ✅ NOVO: Tamanho comprimido (depois da compressão)
        [Required]
        public long TamanhoComprimidoBytes { get; set; }

        // ⚠️ DEPRECATED: Manter por compatibilidade, mas não usar mais
        [StringLength(500)]
        public string? CaminhoArquivo { get; set; }

        // ⚠️ DEPRECATED: Usar TamanhoOriginalBytes
        public long TamanhoBytes { get; set; }

        [Required]
        public DateTime DataUpload { get; set; }

        // Navegação
        public Agendamento? Agendamento { get; set; }
        public DocumentoSolicitado? DocumentoSolicitado { get; set; }
    }
}