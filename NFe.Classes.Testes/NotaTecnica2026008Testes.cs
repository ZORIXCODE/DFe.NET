using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using DFe.Classes.Flags;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Informacoes.Total;

namespace NFe.Classes.Testes;

/// <summary>Testes dos modelos XML acrescentados pela NT 2026.008.</summary>
[TestClass]
public class NotaTecnica2026008Testes
{
    private const string NamespaceNFe = "http://www.portalfiscal.inf.br/nfe";

    [TestMethod]
    public void DadoItemComValoresLiquidosQuandoSerializarEntaoDeveRespeitarOrdemEPrecisao()
    {
        var item = Produto();
        item.vUnComLiq = 1.1234567891m;
        item.vProdLiq = 2.25m;

        var xml = Serializar(item, "prod");

        Assert.AreEqual("1.1234567891", xml.Element(XName.Get("vUnComLiq", NamespaceNFe))?.Value);
        Assert.AreEqual("2.25", xml.Element(XName.Get("vProdLiq", NamespaceNFe))?.Value);
        ValidarFragmento(xml, "prod", false);
    }

    [TestMethod]
    public void DadoItemSemValoresLiquidosQuandoSerializarEntaoDeveOmitirOsDoisCampos()
    {
        var xml = Serializar(Produto(), "prod");

        Assert.IsNull(xml.Element(XName.Get("vUnComLiq", NamespaceNFe)));
        Assert.IsNull(xml.Element(XName.Get("vProdLiq", NamespaceNFe)));
    }

    [TestMethod]
    public void DadoSomenteUmValorLiquidoQuandoValidarEntaoSchemaDeveRejeitar()
    {
        var item = Produto();
        item.vUnComLiq = 1m;
        var xml = Serializar(item, "prod");

        ValidarFragmento(xml, "prod", true);
    }

    [TestMethod]
    public void DadoIcmsPrevistoQuandoSerializarEntaoDeveGerarGrupoEValor()
    {
        var xml = Serializar(new ICMS { TipoICMS = new ICMSPrevistoPagtoAntecip { vICMSPrevisto = 0m } }, "ICMS");

        Assert.AreEqual("0.00", xml.Element(XName.Get("ICMSPrevistoPagtoAntecip", NamespaceNFe))
            ?.Element(XName.Get("vICMSPrevisto", NamespaceNFe))?.Value);
        ValidarFragmento(xml, "ICMS", false);
    }

    [TestMethod]
    public void DadoTotalLiquidoQuandoSerializarEntaoDeveGerarCampoAntesDoIcmsTot()
    {
        var xml = Serializar(new total { vProdLiqTot = 10.25m, ICMSTot = new ICMSTot() }, "total");

        Assert.AreEqual("vProdLiqTot", xml.Elements().First().Name.LocalName);
        Assert.AreEqual("10.25", xml.Elements().First().Value);
    }

    [TestMethod]
    public void DadasReferenciasGovernamentaisQuandoSerializarEntaoDevePreservarOrdemEQuantidade()
    {
        var compras = new gCompraGov
        {
            tpEnteGov = TpEnteGov.Uniao,
            tpOperGov = TpOperGov.FornecimentoPagamentoRealizado,
            refDFeAnt = new List<string> { new('1', 44), new('2', 44) }
        };

        var xml = Serializar(compras, "gCompraGov");

        CollectionAssert.AreEqual(new[] { "1", "2" }, xml.Elements(XName.Get("refDFeAnt", NamespaceNFe))
            .Select(elemento => elemento.Value[0].ToString()).ToArray());
        Assert.AreEqual("3", xml.Element(XName.Get("tpOperGov", NamespaceNFe))?.Value);
        Assert.AreEqual("tpOperGov", xml.Elements().Skip(2).First().Name.LocalName);
        ValidarFragmento(xml, "gCompraGov", false);
    }

    [TestMethod]
    public void DadaOperacaoGovernamentalQuatroQuandoSerializarEntaoDeveOmitirReferencias()
    {
        var xml = Serializar(new gCompraGov { tpOperGov = TpOperGov.RecebimentoFornecimentoPosterior }, "gCompraGov");

        Assert.AreEqual("4", xml.Element(XName.Get("tpOperGov", NamespaceNFe))?.Value);
        Assert.IsFalse(xml.Elements(XName.Get("refDFeAnt", NamespaceNFe)).Any());
        ValidarFragmento(xml, "gCompraGov", false);
    }

    private static XElement Serializar<T>(T objeto, string nome)
    {
        var serializador = new XmlSerializer(typeof(T), new XmlRootAttribute(nome) { Namespace = NamespaceNFe });
        using var escrita = new StringWriter();
        serializador.Serialize(escrita, objeto);
        return XElement.Parse(escrita.ToString());
    }

    private static prod Produto() => new()
    {
        cProd = "1", cEAN = "SEM GTIN", xProd = "PRODUTO TESTE", NCM = "22021000", CFOP = 5102,
        uCom = "UN", qCom = 2, vUnCom = 10, vProd = 20, cEANTrib = "SEM GTIN", uTrib = "UN",
        qTrib = 2, vUnTrib = 10, indTot = (IndicadorTotal)1
    };

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
        if (deveRejeitar) Assert.IsTrue(erros.Count > 0, "O schema aceitou apenas um dos valores líquidos.");
        else Assert.AreEqual(0, erros.Count, string.Join(Environment.NewLine, erros));
    }
}
