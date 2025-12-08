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
```PHP
$wsResponse = Http::timeout(120)->post(http://{{IP}}:5000/api/signing/assinar, [
'signedXmlContent' => $this->xml,
'CnpjEmissor'      => $this->dps['CNPJ'],
'Uri'              => sefin.producaorestrita.nfse.gov.br,
]);
```

**EXEMPLO DE DPS**
```xml
<?xml version="1.0" encoding="utf-8"?>
<DPS xmlns="http://www.sped.fazenda.gov.br/nfse" versao="1.00">
<infDPS Id="{{$infDPS}}">
<tpAmb>{{$tpAmb}}</tpAmb>
<dhEmi>{{$dhEmi}}</dhEmi>
<verAplic>POC_0.0.0</verAplic>
<serie>{{$serie}}</serie>
<nDPS>{{$nDPS}}</nDPS>
<dCompet>{{$dCompet}}</dCompet>
<tpEmit>1</tpEmit>
<cLocEmi>{{$cLocEmi}}</cLocEmi>
<prest>
<CNPJ>{{$CNPJ}}</CNPJ>
<fone>{{$fone}}</fone>
<email>{{$email}}</email>
<regTrib>
<opSimpNac>3</opSimpNac>
<regApTribSN>1</regApTribSN>
<regEspTrib>0</regEspTrib>
</regTrib>
</prest>
<toma>
<CNPJ>{{$toma_CNPJ}}</CNPJ>
<IM>{{$toma_IM}}</IM>
<xNome>{{$toma_xNome}}</xNome>
<end>
<endNac>
<cMun>{{$toma_cMun}}</cMun>
<CEP>{{$toma_CEP}}</CEP>
</endNac>
<xLgr>{{$toma_xLgr}}</xLgr>
<nro>{{$toma_nro}}</nro>
<xBairro>{{$toma_xBairro}}</xBairro>
</end>
</toma>
<serv>
<locPrest>
<cLocPrestacao>{{$cLocPrestacao}}</cLocPrestacao>
</locPrest>
<cServ>
<cTribNac>{{$cTribNac}}</cTribNac>
<cTribMun>{{$cTribMun}}</cTribMun>
<xDescServ>{{$xDescServ}}</xDescServ>
<cNBS>{{$cNBS}}</cNBS>
</cServ>
</serv>
<valores>
<vServPrest>
<vServ>{{$vServ}}</vServ>
</vServPrest>
<trib>
<tribMun>
<tribISSQN>1</tribISSQN>
<tpRetISSQN>1</tpRetISSQN>
</tribMun>
<totTrib>
<pTotTribSN>{{$pTotTribSN}}</pTotTribSN>
</totTrib>
</trib>
</valores>
</infDPS>
</DPS>
```
     
Recebe um XML bruto e retorna o XML assinado.
