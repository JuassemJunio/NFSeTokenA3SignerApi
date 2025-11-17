namespace NFSeTokenA3SignerApi.Models
{
    public class SendingRequest
    {
        public string CnpjEmissor { get; set; }
        public string signedXmlContent { get; set; } = string.Empty;
        public string Uri { get; set; }
        public string chaveAcesso { get; set; } = string.Empty;
    }
}