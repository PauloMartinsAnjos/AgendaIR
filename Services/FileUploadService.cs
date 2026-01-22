namespace AgendaIR.Services
{
    /// <summary>
    /// Serviço responsável por fazer upload e gerenciar arquivos de documentos
    /// Lida com validação, armazenamento e organização dos arquivos
    /// </summary>
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        // Tamanho máximo permitido para upload (10MB em bytes)
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        // Extensões de arquivo permitidas
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        /// <summary>
        /// Construtor que recebe dependências injetadas
        /// </summary>
        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Faz upload de um arquivo e retorna o caminho onde foi salvo
        /// </summary>
        /// <param name="file">Arquivo enviado pelo usuário</param>
        /// <param name="agendamentoId">ID do agendamento (usado para organizar pastas)</param>
        /// <returns>Caminho relativo do arquivo salvo</returns>
        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadFileAsync(IFormFile file, int agendamentoId)
        {
            try
            {
                // Validar se o arquivo foi enviado
                if (file == null || file.Length == 0)
                {
                    return (false, null, "Nenhum arquivo foi enviado");
                }

                // Validar tamanho do arquivo
                if (file.Length > MaxFileSize)
                {
                    return (false, null, $"Arquivo muito grande. Tamanho máximo: {MaxFileSize / 1024 / 1024}MB");
                }

                // Validar extensão do arquivo
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return (false, null, $"Tipo de arquivo não permitido. Permitidos: {string.Join(", ", _allowedExtensions)}");
                }

                // Criar estrutura de pastas: uploads/agendamento_{id}/
                var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", $"agendamento_{agendamentoId}");
                
                // Criar pasta se não existir
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Gerar nome único para o arquivo
                // Formato: {timestamp}_{guid}{extensão}
                var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                // Salvar o arquivo no disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Retornar caminho relativo (sem wwwroot)
                var relativePath = Path.Combine("uploads", $"agendamento_{agendamentoId}", uniqueFileName);
                
                _logger.LogInformation($"Arquivo {file.FileName} salvo com sucesso em {relativePath}");
                
                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo");
                return (false, null, "Erro ao salvar arquivo: " + ex.Message);
            }
        }

        /// <summary>
        /// Deleta um arquivo do servidor
        /// </summary>
        /// <param name="relativePath">Caminho relativo do arquivo</param>
        public bool DeleteFile(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Arquivo deletado: {relativePath}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao deletar arquivo: {relativePath}");
                return false;
            }
        }

        /// <summary>
        /// Obtém o tamanho formatado de um arquivo em bytes
        /// </summary>
        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
