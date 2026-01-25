namespace AgendaIR.Services
{
    /// <summary>
    /// Serviço responsável por gerar tokens mágicos (magic tokens) para autenticação de clientes
    /// Um magic token é um identificador único que permite login automático sem senha
    /// </summary>
    public class MagicLinkService
    {
        /// <summary>
        /// Gera um token único para o cliente
        /// O token é uma combinação de GUID (identificador único) + timestamp
        /// Isso garante que cada token seja único e praticamente impossível de adivinhar
        /// </summary>
        /// <returns>String com o token gerado</returns>
        public string GerarMagicToken()
        {
            // GUID (Globally Unique Identifier) é um identificador único de 128 bits
            // Ele é gerado aleatoriamente e a chance de duplicação é quase zero
            var guid = Guid.NewGuid().ToString("N"); // "N" remove os hífens

            // Ticks são o número de intervalos de 100 nanossegundos desde 01/01/0001
            // Adicionar isso garante ainda mais unicidade baseada no tempo
            var timestamp = DateTime.UtcNow.Ticks.ToString();

            // Combinar GUID + timestamp para máxima segurança
            return $"{guid}{timestamp}";
        }

        /// <summary>
        /// Gera o link completo que será enviado ao cliente via WhatsApp
        /// </summary>
        /// <param name="token">Token único do cliente</param>
        /// <param name="baseUrl">URL base do site (ex: https://seusite.com)</param>
        /// <returns>URL completa para login</returns>
        public string GerarMagicLink(string token, string baseUrl)
        {
            // Remove barra final da URL se existir
            baseUrl = baseUrl.TrimEnd('/');

            // Retorna o link no formato: https://seusite.com/Agendamentos/AcessarLinkMagico?token=abc123...
            return $"{baseUrl}/Agendamentos/AcessarLinkMagico?token={token}";
        }

        /// <summary>
        /// Valida se um token está no formato correto
        /// </summary>
        /// <param name="token">Token a ser validado</param>
        /// <returns>True se válido, False caso contrário</returns>
        public bool ValidarToken(string? token)
        {
            // Token não pode ser nulo ou vazio
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // Token deve ter um comprimento mínimo (GUID tem 32 chars + timestamp)
            if (token.Length < 40)
                return false;

            return true;
        }
    }
}
