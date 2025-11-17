# NFSeTokenA3SignerApi

# NFSe Token A3 Signer API 🔐

![Net](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

Esta API foi desenvolvida para simplificar o processo de assinatura digital de Notas Fiscais de Serviço Eletrônica (NFS-e) utilizando certificados digitais do tipo **Token A3**.

Devido à natureza dos certificados A3 (que residem em hardware criptográfico), esta API atua como um middleware local, expondo endpoints para que aplicações web ou outros serviços possam solicitar assinaturas digitais sem interagir diretamente com os drivers do token.

## 🚀 Funcionalidades

-   🔌 **Interação com Token A3:** Comunicação com o hardware criptográfico para realizar assinaturas.
-   📝 **Assinatura de XML:** Recebe o XML da nota, assina e retorna o documento válido.
-   🌐 **API RESTful:** Endpoints padronizados para fácil integração.
-   📄 **Swagger:** Documentação interativa dos endpoints gerada automaticamente.

## 📋 Pré-requisitos

Para executar este projeto, você precisará de:

* [.NET SDK](https://dotnet.microsoft.com/download) (versão compatível com o projeto, ex: .NET 6/7/8).
* Drivers do seu Token A3 instalados e funcionais na máquina.
* Token A3 conectado à porta USB.

## 🔧 Instalação e Execução

1.  **Clone o repositório:**
    ```bash
    git clone [https://github.com/JuassemJunio/NFSeTokenA3SignerApi.git](https://github.com/JuassemJunio/NFSeTokenA3SignerApi.git)
    cd NFSeTokenA3SignerApi
    ```

2.  **Restaure as dependências:**
    ```bash
    dotnet restore
    ```

3.  **Execute a aplicação:**
    > **Nota:** Para acessar o Token A3, é recomendável executar a aplicação como Console/User Session, e não como um serviço do Windows isolado, para garantir acesso ao Smart Card.
    ```bash
    dotnet run
    ```

4.  **Acesse a documentação:**
    Abra o navegador e vá para:
    `https://localhost:7194/swagger` (ou a porta configurada no seu console).

## 📡 Endpoints Principais

Abaixo um exemplo de como utilizar o serviço (baseado na estrutura comum de APIs de assinatura):

### 1. Assinar XML
**POST** `/api/Signer/Sign`

Recebe um XML bruto e retorna o XML assinado.

**Corpo da Requisição (Exemplo):**
```json
{
  "xmlContent": "<Rps>...</Rps>",
  "certificateThumbprint": "Opcional se houver apenas um cert"
}
