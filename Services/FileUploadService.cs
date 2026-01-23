using System.IO.Compression;

namespace AgendaIR.Services
{
    /// <summary>
    /// Serviço para processar upload de arquivos
    /// Agora comprime arquivos antes de salvar no banco de dados
    /// </summary>
    public class FileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        public FileUploadService(ILogger<FileUploadService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processa o arquivo: valida, lê e comprime
        /// </summary>
        public async Task<FileUploadResult> ProcessarArquivoAsync(IFormFile arquivo)
        {
            try
            {
                // Validar tamanho
                if (arquivo.Length > _maxFileSize)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        ErrorMessage = $"O arquivo excede o tamanho máximo de {_maxFileSize / 1024 / 1024}MB"
                    };
                }

                // Validar extensão
                var extension = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        ErrorMessage = $"Extensão {extension} não permitida. Use: {string.Join(", ", _allowedExtensions)}"
                    };
                }

                // Ler arquivo para memória
                byte[] conteudoOriginal;
                using (var memoryStream = new MemoryStream())
                {
                    await arquivo.CopyToAsync(memoryStream);
                    conteudoOriginal = memoryStream.ToArray();
                }

                // Comprimir usando GZip
                byte[] conteudoComprimido = ComprimirArquivo(conteudoOriginal);

                var taxaCompressao = (1 - ((double)conteudoComprimido.Length / conteudoOriginal.Length)) * 100;

                _logger.LogInformation(
                    $"✓ Arquivo '{arquivo.FileName}' processado: " +
                    $"Original: {conteudoOriginal.Length:N0} bytes, " +
                    $"Comprimido: {conteudoComprimido.Length:N0} bytes " +
                    $"(redução de {taxaCompressao:F1}%)"
                );

                return new FileUploadResult
                {
                    Success = true,
                    ConteudoComprimido = conteudoComprimido,
                    TamanhoOriginal = conteudoOriginal.Length,
                    TamanhoComprimido = conteudoComprimido.Length,
                    NomeArquivo = arquivo.FileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar arquivo '{arquivo.FileName}'");
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Erro ao processar arquivo: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Comprime array de bytes usando GZip
        /// </summary>
        private byte[] ComprimirArquivo(byte[] dados)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(dados, 0, dados.Length);
            }
            return outputStream.ToArray();
        }

        /// <summary>
        /// Descomprime array de bytes usando GZip
        /// </summary>
        public byte[] DescomprimirArquivo(byte[] dadosComprimidos)
        {
            using var inputStream = new MemoryStream(dadosComprimidos);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }

    /// <summary>
    /// Resultado do processamento de upload
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // ✅ NOVOS campos para trabalhar com bytes
        public byte[]? ConteudoComprimido { get; set; }
        public long TamanhoOriginal { get; set; }
        public long TamanhoComprimido { get; set; }
        public string? NomeArquivo { get; set; }

        // ⚠️ DEPRECATED - manter por compatibilidade
        public string? FilePath { get; set; }
    }
}