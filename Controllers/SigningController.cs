using System.IO.Compression;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NFSeTokenA3SignerApi.Models;

namespace NFSeTokenA3SignerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SigningController : ControllerBase
    {
        // ===========================
        // 🔒 MÉTODO DE ASSINATURA A3
        // ===========================
        [HttpPost("assinar")]
        public IActionResult AssinarXml([FromBody] SigningRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.XmlContent))
                    return BadRequest(new { error = "Conteúdo XML ou PIN do certificado ausente." });

                string signedXml = PerformA3Signing(request.XmlContent, request.IdReference, request.CnpjEmissor);

                return Ok(new
                {
                    success = true,
                    signed_xml = signedXml
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erro no processo de assinatura A3.",
                    details = ex.Message
                });
            }
        }

        // ==========================================================
        // 🇧🇷 CONSULTA NFSE PELO AMBIENTE NACIONAL (DPS / GOV.BR) - REST
        // ==========================================================
        [HttpPost("consultar-nfse-nacional")]
        public async Task<IActionResult> ConsultarNfseNacional([FromBody] SendingRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.chaveAcesso))
                return BadRequest(new { error = "A chave de acesso é obrigatória." });

            try
            {
                using var httpClient = await CreateHttpClientAsync(request);
                var response = await httpClient.GetAsync($"nfse/{request.chaveAcesso}");
                var responseBody = await response.Content.ReadAsStringAsync();

                var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<NFSeResponse>(responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        error = "Falha ao consultar NFSe no ambiente nacional.",
                        status = (int)response.StatusCode,
                        detalhes = jsonResponse?.erros ?? responseBody
                    });
                }

                // Descompacta o XML retornado
                var gzippedBytes = Convert.FromBase64String(jsonResponse?.nfseXmlGZipB64 ?? string.Empty);
                await using var compressedStream = new MemoryStream(gzippedBytes);
                await using var decompressedStream = new MemoryStream();
                await using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    await gzip.CopyToAsync(decompressedStream);
                }

                var xml = Encoding.UTF8.GetString(decompressedStream.ToArray());

                // Converte XML → JSON
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var jsonString = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                return Ok(JsonDocument.Parse(jsonString));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erro interno ao consultar NFSe.",
                    details = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }


        // ==========================================================
        // 🇧🇷 ENVIO PARA O AMBIENTE NACIONAL (DPS / GOV.BR) - REST
        // ==========================================================
        [HttpPost("enviar-nfse-nacional")]
        public async Task<IActionResult> EnviarNfseNacional([FromBody] SendingRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.signedXmlContent))
                return BadRequest(new { error = "Conteúdo do XML assinado é obrigatório." });

            try
            {
                var xmlContent = request.signedXmlContent;
                var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
                var gzippedBase64 = CompressToBase64(xmlBytes);

                var payload = new { dpsXmlGZipB64 = gzippedBase64 };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);

                using var httpClient = await CreateHttpClientAsync(request);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("nfse", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<NFSeResponse>(responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        error = "Falha ao enviar NFSe para o ambiente nacional.",
                        status = (int)response.StatusCode,
                        detalhes = jsonResponse?.erros ?? responseBody
                    });
                }

                if (jsonResponse?.erros != null)
                {
                    return BadRequest(new { error = "Erro retornado pelo ambiente NFSe.", detalhes = jsonResponse.erros });
                }

                var gzippedBytes = Convert.FromBase64String(jsonResponse?.nfseXmlGZipB64 ?? string.Empty);
                await using var compressedStream = new MemoryStream(gzippedBytes);
                await using var decompressedStream = new MemoryStream();
                await using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    await gzip.CopyToAsync(decompressedStream);
                }

                var xml = Encoding.UTF8.GetString(decompressedStream.ToArray());
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var jsonString = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                return Ok(JsonDocument.Parse(jsonString));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erro interno ao enviar NFSe.",
                    details = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }



        public static string DescomprimirGZIP(byte[] bytes)
        {
            var retorno = String.Empty;
            var texto = Encoding.UTF8.GetString(bytes);
            byte[] inputBytes = Convert.FromBase64String(texto);

            using (var inputStream = new MemoryStream(inputBytes))
            using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var streamReader = new StreamReader(gZipStream))
            {
                retorno = streamReader.ReadToEnd();
            }

            return retorno;
        }

        // ==========================================================
        // 🔧 CRIAÇÃO DO HTTP CLIENT COM CERTIFICADO DIGITAL
        // ==========================================================
        private async Task<HttpClient> CreateHttpClientAsync(SendingRequest request)
        {
            var cert = FindCertificateByCnpj(request.CnpjEmissor)
                ?? throw new InvalidOperationException("Certificado da empresa não encontrado.");

            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12
            };

            handler.ClientCertificates.Add(cert);

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(request.Uri)
            };
        }



        // ==========================================================
        // 🇧🇷 VERIFICA SE O DPS JÁ EXISTE (AMBIENTE NACIONAL - GOV.BR)
        // ==========================================================
        [HttpGet("verificar-dps")]
        public async Task<IActionResult> VerificarDpsNacional(
     [FromQuery] string dpsId,
     [FromQuery] string cnpjEmissor,
     [FromQuery] string uri)
        {
            if (string.IsNullOrWhiteSpace(dpsId))
                return BadRequest(new { error = "O identificador do DPS é obrigatório." });

            try
            {
                var request = new SendingRequest
                {
                    CnpjEmissor = cnpjEmissor,
                    Uri = uri
                };

                using var client = await CreateHttpClientAsync(request);
                var endpoint = $"dps/{dpsId}";

                var response = await client.GetAsync(endpoint);
                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<NFSeResponse>(body, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result == null)
                        {
                            return Ok(new
                            {
                                message = "Identificador do DPS encontrado, mas a resposta está vazia."
                            });
                        }

                        return Ok(new
                        {
                            message = "Identificador do DPS encontrado e processado com sucesso.",
                            dados = result
                        });
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Caso o corpo não esteja em JSON válido
                        return Ok(new
                        {
                            message = "Identificador do DPS encontrado, mas não foi possível ler o conteúdo retornado.",
                            rawResponse = body
                        });
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound(new { error = "Identificador do DPS não encontrado." });
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return BadRequest(new { error = "Identificador de DPS inválido." });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        error = "Erro inesperado ao consultar o DPS.",
                        detalhes = body
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erro interno ao verificar DPS.",
                    details = ex.Message
                });
            }
        }



        // ==========================================================
        // 🧰 UTILITÁRIO: COMPACTA E CONVERTE PARA BASE64
        // ==========================================================
        private static string CompressToBase64(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return Convert.ToBase64String(output.ToArray());
        }

        // ==========================================================
        // 🔍 BUSCA CERTIFICADO PELO CNPJ
        // ==========================================================
        private X509Certificate2 FindCertificateByCnpj(string cnpjEmissor)
        {
            string cnpjLimpo = cnpjEmissor.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2 cert = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var c in store.Certificates)
                {
                    if (c.HasPrivateKey && c.Subject.Contains(cnpjLimpo))
                    {
                        cert = c;
                        break;
                    }
                }
            }
            finally
            {
                store.Close();
            }

            return cert;
        }

        // ==========================================================
        // ✍️ ASSINATURA XML COM CERTIFICADO A3
        // ==========================================================
        private string PerformA3Signing(string xml, string id, string cnpjEmissor)
        {
            string cnpjLimpo = cnpjEmissor.Replace(".", "").Replace("/", "").Replace("-", "").Trim();

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            doc.LoadXml(xml);

            // Remove assinatura anterior, se existir
            XmlNodeList signatures = doc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (signatures.Count > 0)
                signatures[0].ParentNode.RemoveChild(signatures[0]);

            XmlNode targetNode = doc.SelectSingleNode($"//*[local-name()='infDPS' and @Id='{id}']");

            if (targetNode == null)
                throw new Exception($"Elemento com Id='{id}' não encontrado no XML.");

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2 cert = null;
            foreach (var c in store.Certificates)
            {
                if (c.HasPrivateKey && c.Subject.Contains(cnpjLimpo))
                {
                    cert = c;
                    break;
                }
            }
            store.Close();

            if (cert == null)
                throw new CryptographicException($"Certificado para o CNPJ {cnpjEmissor} não encontrado.");

            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.GetRSAPrivateKey();
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

            Reference reference = new Reference($"#{id}");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            signedXml.AddReference(reference);

            signedXml.KeyInfo = new KeyInfo();
            signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert));

            signedXml.ComputeSignature();
            XmlElement signatureElement = signedXml.GetXml();
            doc.DocumentElement.AppendChild(signatureElement);

            return doc.OuterXml;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok", message = "API local funcionando" });
        }
    }
}
