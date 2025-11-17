namespace NFSeTokenA3SignerApi.Models
{
    public class NFSeResponse
    {
        public string idDps { get; set; } = string.Empty;
        public string chaveAcesso { get; set; } = string.Empty;
        public string nfseXmlGZipB64 { get; set; } = string.Empty;
        public object? erros { get; set; }
    }
}
