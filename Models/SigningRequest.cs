namespace NFSeTokenA3SignerApi.Models
{
    public class SigningRequest
    {
        public string XmlContent { get; set; }

        public string IdReference { get; set; }

        public string? XpathTarget { get; set; }

        public string CnpjEmissor { get; set; }
    }
}