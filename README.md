# NFSe Token A3 Signer API

Esta API foi desenvolvida para simplificar o processo de assinatura digital de Notas Fiscais nacional de Serviço Eletrônica (NFS-e) utilizando certificados digitais do tipo **Token A3**.

Devido à natureza dos certificados A3 (que residem em hardware criptográfico), esta API atua como um middleware local, expondo endpoints para que aplicações web ou outros serviços possam solicitar assinaturas digitais sem interagir diretamente com os drivers do token.

## Funcionalidades

-    **Interação com Token A3:** Comunicação com o hardware criptográfico para realizar assinaturas.
-    **Assinatura de XML:** Recebe o XML da nota, assina e retorna o documento válido.
-    **API RESTful:** Endpoints padronizados para fácil integração.

## Pré-requisitos

Para executar este projeto, você precisará de:

* [.NET SDK](https://dotnet.microsoft.com/download) (versão compatível com o projeto, ex: .NET 10).
* Drivers do seu Token A3 instalados e funcionais na máquina.
* Token A3 conectado à porta USB.
  
## Endpoints Principais

Abaixo um exemplo de como utilizar o serviço (baseado na estrutura comum de APIs de assinatura):

### 1. Assinar XML em PHP
**POST**
* 
* $wsResponse = Http::timeout(120)->post(http://{{IP}}:5000/api/signing/assinar, [
* 'signedXmlContent' => $this->xml,
* 'CnpjEmissor'      => $this->dps['CNPJ'],
* 'Uri'              => sefin.producaorestrita.nfse.gov.br,
* ]);
     
Recebe um XML bruto e retorna o XML assinado.
