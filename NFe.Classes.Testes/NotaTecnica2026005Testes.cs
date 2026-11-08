/********************************************************************************/
/* Projeto: Biblioteca ZeusNFe                                                  */
/* Biblioteca C# para emissão de Nota Fiscal Eletrônica - NFe e Nota Fiscal de  */
/* Consumidor Eletrônica - NFC-e (http://www.nfe.fazenda.gov.br)                */
/*                                                                              */
/* Direitos Autorais Reservados (c) 2014 Adenilton Batista da Silva             */
/*                                       Zeusdev Tecnologia LTDA ME             */
/*                                                                              */
/*  Esta biblioteca é software livre; você pode redistribuí-la e/ou modificá-la */
/* sob os termos da Licença Pública Geral Menor do GNU conforme publicada pela  */
/* Free Software Foundation; tanto a versão 2.1 da Licença, ou (a seu critério) */
/* qualquer versão posterior. Consulte LICENSE.TXT para mais detalhes.         */
/********************************************************************************/

using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using DFe.Classes.Flags;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.Tipos;
using NFe.Classes.Informacoes.Emitente;
using NFe.Classes.Informacoes.Total.IbsCbs;
using GrupoSimplesItem = NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.gTribSN;
using GrupoSimplesTotal = NFe.Classes.Informacoes.Total.IbsCbs.gTribSN;
using GrupoIbsCbs = NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.gIBSCBS;
using GrupoIbsUf = NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesIbs.gIBSUF;
using GrupoIbsMun = NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesIbs.gIBSMun;
using GrupoCbs = NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesCbs.gCBS;

namespace NFe.Classes.Testes;

/// <summary>Testes dos modelos da NT 2026.005 para NF-e e NFC-e.</summary>
[TestClass]
public class NotaTecnica2026005Testes
{
    private const string NamespaceNFe = "http://www.portalfiscal.inf.br/nfe";

    /// <summary>Verifica os códigos opcionais no emitente e no produto.</summary>
    [TestMethod]
    public void DadoSimplesNacionalQuandoSerializarEntaoDeveInformarRegimeEAtividade()
    {
        var emitente = Serializar(new emit
        {
            CRT = CRT.SimplesNacional,
            tpRegimeIBSCBS = TipoRegimeIbsCbs.SimplesNacionalRegimeRegular
        }, "emit");
        var produto = Produto();
        produto.cAtivSN = "01";
        var xmlProduto = Serializar(produto, "prod");

        Assert.AreEqual("1", emitente.Element(XName.Get("tpRegimeIBSCBS", NamespaceNFe))?.Value);
        Assert.AreEqual("01", xmlProduto.Element(XName.Get("cAtivSN", NamespaceNFe))?.Value);
        ValidarFragmento(xmlProduto, "prod", false);
    }

    /// <summary>Verifica que operações sem os campos novos mantêm a serialização anterior.</summary>
    [TestMethod]
    public void DadoRegimeSemOpcaoQuandoSerializarEntaoDeveOmitirCamposDoSimples()
    {
        var emitente = Serializar(new emit { CRT = CRT.RegimeNormal }, "emit");
        var produto = Serializar(Produto(), "prod");
        var imposto = Serializar(new IBSCBS { CST = CST.Cst000, cClassTrib = "000001" }, "IBSCBS");
        var total = Serializar(new IBSCBSTot { vBCIBSCBS = 0 }, "IBSCBSTot");

        Assert.IsNull(emitente.Element(XName.Get("tpRegimeIBSCBS", NamespaceNFe)));
        Assert.IsNull(produto.Element(XName.Get("cAtivSN", NamespaceNFe)));
        Assert.IsNull(imposto.Element(XName.Get("gTribSN", NamespaceNFe)));
        Assert.IsNull(total.Element(XName.Get("gTribSN", NamespaceNFe)));
    }

    /// <summary>Verifica valores, ordem e schema dos grupos do Simples Nacional.</summary>
    [TestMethod]
    public void DadoGrupoSimplesQuandoSerializarEntaoDeveRespeitarOrdemValoresESchema()
    {
        var imposto = Serializar(new IBSCBS
        {
            CST = CST.Cst000,
            cClassTrib = "000001",
            gTribSN = new GrupoSimplesItem
            {
                gIBSSN = new gIBSSN { pIBSSN = 0.12346m, vIBSSN = 1.234m },
                gCBSSN = new gCBSSN { pCBSSN = 0.98766m, vCBSSN = 9.876m }
            }
        }, "IBSCBS");
        var total = Serializar(new IBSCBSTot
        {
            vBCIBSCBS = 100,
            gTribSN = new GrupoSimplesTotal { vIBSSN = 1.23m, vCBSSN = 9.88m }
        }, "IBSCBSTot");

        var grupo = imposto.Element(XName.Get("gTribSN", NamespaceNFe));
        Assert.IsNotNull(grupo);
        CollectionAssert.AreEqual(new[] { "gIBSSN", "gCBSSN" }, grupo.Elements().Select(x => x.Name.LocalName).ToArray());
        Assert.AreEqual("0.1235", grupo.Element(XName.Get("gIBSSN", NamespaceNFe))?.Element(XName.Get("pIBSSN", NamespaceNFe))?.Value);
        Assert.AreEqual("1.23", grupo.Element(XName.Get("gIBSSN", NamespaceNFe))?.Element(XName.Get("vIBSSN", NamespaceNFe))?.Value);
        Assert.AreEqual("0.9877", grupo.Element(XName.Get("gCBSSN", NamespaceNFe))?.Element(XName.Get("pCBSSN", NamespaceNFe))?.Value);
        Assert.AreEqual("9.88", total.Element(XName.Get("gTribSN", NamespaceNFe))?.Element(XName.Get("vCBSSN", NamespaceNFe))?.Value);
        ValidarFragmento(imposto, "IBSCBS", false);
        ValidarFragmento(total, "IBSCBSTot", false);
    }

    /// <summary>Verifica que o grupo informado exige a composição da CBS.</summary>
    [TestMethod]
    public void DadoGrupoSimplesSemCbsQuandoValidarEntaoSchemaDeveRejeitar()
    {
        var imposto = Serializar(new IBSCBS
        {
            CST = CST.Cst000,
            cClassTrib = "000001",
            gTribSN = new GrupoSimplesItem { gIBSSN = new gIBSSN { pIBSSN = 0, vIBSSN = 0 } }
        }, "IBSCBS");

        ValidarFragmento(imposto, "IBSCBS", true);
    }

    /// <summary>Verifica o cenário do CRT 2, em que o IBS regular convive com a CBS do Simples.</summary>
    [TestMethod]
    public void DadoIbsRegularECbsSimplesQuandoSerializarEntaoDeveValidarSchema()
    {
        var imposto = Serializar(new IBSCBS
        {
            CST = CST.Cst000,
            cClassTrib = "000001",
            gIBSCBS = new GrupoIbsCbs
            {
                vBC = 100,
                gIBSUF = new GrupoIbsUf
                {
                    pIBSUF = 0.1m,
                    vIBSUF = 0.1m
                },
                gIBSMun = new GrupoIbsMun
                {
                    pIBSMun = 0,
                    vIBSMun = 0
                },
                vIBS = 0.1m,
                gCBS = new GrupoCbs
                {
                    pCBS = 0,
                    vCBS = 0
                }
            },
            gTribSN = new GrupoSimplesItem
            {
                gCBSSN = new gCBSSN { pCBSSN = 0.0000m, vCBSSN = 0.00m }
            }
        }, "IBSCBS");

        Assert.AreEqual("0.0000", imposto.Element(XName.Get("gTribSN", NamespaceNFe))
            ?.Element(XName.Get("gCBSSN", NamespaceNFe))
            ?.Element(XName.Get("pCBSSN", NamespaceNFe))?.Value);
        ValidarFragmento(imposto, "IBSCBS", false);
    }

    /// <summary>Serializa um objeto XML da NF-e.</summary>
    private static XElement Serializar<T>(T objeto, string nome)
    {
        var serializador = new XmlSerializer(typeof(T), new XmlRootAttribute(nome) { Namespace = NamespaceNFe });
        using var escrita = new StringWriter();
        serializador.Serialize(escrita, objeto);
        return XElement.Parse(escrita.ToString());
    }

    /// <summary>Cria um produto mínimo válido no schema.</summary>
    private static prod Produto() => new()
    {
        cProd = "1", cEAN = "SEM GTIN", xProd = "PRODUTO TESTE", NCM = "22021000", CFOP = 5102,
        uCom = "UN", qCom = 1, vUnCom = 10, vProd = 10, cEANTrib = "SEM GTIN", uTrib = "UN",
        qTrib = 1, vUnTrib = 10, indTot = (IndicadorTotal)1
    };

    /// <summary>Valida o fragmento serializado com o schema da NF-e.</summary>
    private static void ValidarFragmento(XElement xml, string nome, bool deveRejeitar)
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta != null && !Directory.Exists(Path.Combine(pasta.FullName, "NFe.AppTeste", "Schemas")))
            pasta = pasta.Parent;
        Assert.IsNotNull(pasta);

        var schemas = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        schemas.Add(NamespaceNFe, Path.Combine(pasta.FullName, "NFe.AppTeste", "Schemas", "nfe_v4.00.xsd"));
        schemas.Compile();
        var raiz = schemas.GlobalElements[new XmlQualifiedName("NFe", NamespaceNFe)];
        var visitados = new HashSet<XmlSchemaObject>();
        XmlSchemaElement? Buscar(XmlSchemaObject? objeto)
        {
            if (objeto == null || !visitados.Add(objeto)) return null;
            if (objeto is XmlSchemaElement elemento)
                return elemento.Name == nome ? elemento : Buscar(elemento.ElementSchemaType);
            if (objeto is XmlSchemaComplexType tipo) return Buscar(tipo.ContentTypeParticle);
            if (objeto is XmlSchemaGroupBase grupo)
                foreach (XmlSchemaObject filho in grupo.Items)
                {
                    var encontrado = Buscar(filho);
                    if (encontrado != null) return encontrado;
                }
            return null;
        }

        var fragmento = Buscar(raiz);
        Assert.IsNotNull(fragmento);
        var erros = new List<string>();
        xml.Validate(fragmento, schemas, (_, evento) => erros.Add(evento.Message), true);
        if (deveRejeitar) Assert.IsTrue(erros.Count > 0, "O schema aceitou um grupo do Simples sem CBS.");
        else Assert.AreEqual(0, erros.Count, string.Join(Environment.NewLine, erros));
    }
}
