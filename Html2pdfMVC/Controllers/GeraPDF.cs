using System;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Collections.Generic;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.html;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.css;
using iTextSharp.tool.xml.pipeline.html;
using iTextSharp.tool.xml.pipeline.end;
using iTextSharp.tool.xml.pipeline.css;
using iTextSharp.tool.xml.pipeline;

using HtmlAgilityPack;

using static iTextSharp.text.Font;

namespace Html2pdfMVC.Controllers {
  public class GeraPDF : ActionResult {
    public GeraPDF(object modelo) {
      this.Modelo = modelo;
    }
    public GeraPDF(string nomeView, object modelo) {
      NomeView = nomeView;
      this.Modelo = modelo;
    }
    public GeraPDF(object modelo, Action<PdfWriter, Document> acao) {
      if (acao == null)
        throw new ArgumentNullException("Instrução inválida!");

      this.Modelo = modelo;
      this.preparaDocumento = acao;
    }
    public GeraPDF(string nomeView, object modelo, Action<PdfWriter, Document> acao) {
      if (acao == null)
        throw new ArgumentNullException("Instrução inválida!");

      this.NomeView = nomeView;
      this.Modelo = modelo;
      this.preparaDocumento = acao;
    }

    // Dados do registro para saída
    public object Modelo { get; set; }

    // Definição de arquivo para download
    public string Download { get; set; }

    // Nome da view a ser renderizada (default = view da ação)
    public string NomeView { get; set; }

    // Define função para efetuar modificações nas definições do documento
    public Action<PdfWriter, Document> preparaDocumento { get; set; }

    // Gera saída http
    public override void ExecuteResult(ControllerContext cc) {
      if (NomeView == null) {
        NomeView = cc.RouteData.GetRequiredString("action");
      }

      // Designa conforme tipo de saída, tela ou download
      cc.Controller.ViewData.Model = Modelo;
      if (cc.HttpContext.Request.Form["saida"] != null &&
          cc.HttpContext.Request.Form["saida"].ToLower().Equals("html")) {
        RenderizaHtml(cc);
      } else {

        // Arquivo para download
        if (Download != null)
          cc.HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + Download);
        byte[] buff = GeraDocumento(cc);
        if (buff != null)
          (new FileContentResult(buff, "application/pdf")).ExecuteResult(cc);
      }
    }

    // Compõe o documento PDF
    public byte[] GeraDocumento(ControllerContext cc) {

      cc.Controller.ViewData.Model = Modelo;

      // Prepara o buffer de saída
      byte[] buff;
      using (Document doc = new Document()) {
        using (MemoryStream ms = new MemoryStream()) {
          PdfWriter pw = PdfWriter.GetInstance(doc, ms);
          pw.CloseStream = false;

          if (preparaDocumento != null) {
            preparaDocumento(pw, doc);
          }
          doc.Open();

          // Converte o HTML em PDF
          using (StringReader sr = new StringReader(StringHtml(cc))) {

              // Versão VahidN (ver classe abaixo)
              var tagProcessors = (DefaultTagProcessorFactory)Tags.GetHtmlTagProcessorFactory();
              tagProcessors.RemoveProcessor(HTML.Tag.IMG); // remove the default processor
              tagProcessors.AddProcessor(HTML.Tag.IMG, new CustomImageTagProcessor());              // use our new processor

              // Campo texto de formuários
              tagProcessors.AddProcessor(HTML.Tag.INPUT, new CustomInputTagProcessor());            // (experimental)
              tagProcessors.AddProcessor(HTML.Tag.SELECT, new CustomSelectTagProcessor());          // (experimental)

              CssFilesImpl cssFiles = new CssFilesImpl();
              cssFiles.Add(XMLWorkerHelper.GetInstance().GetDefaultCSS());
              var cssResolver = new StyleAttrCSSResolver(cssFiles);
              cssResolver.AddCss(@"code { padding: 2px 4px; }", "utf-8", true);

              var charset = Encoding.UTF8;
              var hpc = new HtmlPipelineContext(new CssAppliersImpl(new XMLWorkerFontProvider()));
              hpc.SetAcceptUnknown(true).AutoBookmark(true).SetTagFactory(tagProcessors);           // inject the tagProcessors

              /*
              PdfWriterPipeline pdf = new PdfWriterPipeline(doc, pw);
              CustomPipeline custom = new CustomPipeline(pdf);
              HtmlPipeline htmlPipeline = new HtmlPipeline(hpc, custom);
              */

              var htmlPipeline = new HtmlPipeline(hpc, new PdfWriterPipeline(doc, pw));
              var pipeline = new CssResolverPipeline(cssResolver, htmlPipeline);
              var worker = new XMLWorker(pipeline, true);
              var xmlParser = new XMLParser(true, worker, charset);

            try {
              xmlParser.Parse(sr);

              //XMLWorkerHelper.GetInstance().ParseXHtml(pw, doc, sr);                              // versão simplificada

            } catch (Exception ee) {
              cc.HttpContext.Session["Erro"] = ee;
              try {
                doc.Dispose();
              } catch (Exception) { }
              cc.Controller.TempData["Erro"] = ee;
              NomeView = "Erro";
              RenderizaHtml(cc);
              return null;
            }

            doc.Close();
            buff = ms.ToArray();
          }
        }
      }
      return buff;
    }

    // Prepara conteúdo HTML para saída http sem conversão (para testes)
    private void RenderizaHtml(ControllerContext cc) {
      IView view = ViewEngines.Engines.FindView(cc, NomeView, null).View;
      ViewContext vc = new ViewContext(
        cc,
        view,
        cc.Controller.ViewData,
        cc.Controller.TempData,
        cc.HttpContext.Response.Output);

      // Transpõe conteúdo html para a saída http
      view.Render(vc, cc.HttpContext.Response.Output);
    }

    // Prepara o conteúdo HTML e retorna string
    public string StringHtml(ControllerContext cc) {
      string stResult;
      IView view = ViewEngines.Engines.FindView(cc, NomeView, null).View;
      StringBuilder sb = new StringBuilder();

      // Obtém sequência do Html
      using (TextWriter tw = new StringWriter(sb)) {
        ViewContext vc = new ViewContext(
          cc,
          view,
          cc.Controller.ViewData,
          cc.Controller.TempData,
          tw);
        view.Render(vc, tw);
      }
      stResult = sb.ToString();
      stResult = preparaTags(stResult);
      return stResult;
    }

    // Prepara tags do Html
    public string preparaTags(string html) {
      string stResult = html;

      // Filtra documento
      StringBuilder sb = new StringBuilder();
      StringWriter sw = new StringWriter(sb);
      HtmlDocument had = new HtmlDocument();
      HtmlNode.ElementsFlags["form"] = HtmlElementFlag.CanOverlap;
      HtmlNode.ElementsFlags["br"] = HtmlElementFlag.Empty;
      HtmlNode.ElementsFlags["option"] = HtmlElementFlag.CanOverlap;
      if (HtmlNode.ElementsFlags.ContainsKey("input")) {
        HtmlNode.ElementsFlags["input"] = HtmlElementFlag.Closed;
      } else {
        HtmlNode.ElementsFlags.Add("input", HtmlElementFlag.Closed);
      }
      if (HtmlNode.ElementsFlags.ContainsKey("link")) {
        HtmlNode.ElementsFlags["link"] = HtmlElementFlag.Closed;
      } else {
        HtmlNode.ElementsFlags.Add("link", HtmlElementFlag.Closed);
      }
      if (HtmlNode.ElementsFlags.ContainsKey("img")) {
        HtmlNode.ElementsFlags["img"] = HtmlElementFlag.Closed;
      } else {
        HtmlNode.ElementsFlags.Add("img", HtmlElementFlag.Closed);
      }
      had.OptionOutputAsXml = false;
      had.OptionCheckSyntax = true;
      had.OptionFixNestedTags = true;
      had.OptionAutoCloseOnEnd = false;
      had.OptionWriteEmptyNodes = true;

        try {
        had.LoadHtml(html);
      } catch (Exception ee) {
        System.Diagnostics.Debug.WriteLine("Falha ao abrir html para filtragem (" + ee + ").");
        return html;
      }

      // Adiciona atributos para tratamento posterior
      foreach (HtmlNode no in had.DocumentNode.Descendants("option")) {
        if (no.Attributes.Contains("selected")) {
          no.SetAttributeValue("data-conteudo", no.InnerText);
        }
      }

      had.Save(sw);
      stResult = sb.ToString();
      return stResult;
    }
  }

  /*
   * Tag de imagem customizada conforme artigo de VahidN em
   * http://stackoverflow.com/questions/19389999/can-itextsharp-xmlworker-render-embedded-images 
   */
  public class CustomImageTagProcessor : iTextSharp.tool.xml.html.Image {
    public override IList<IElement> End(IWorkerContext ctx, Tag tag, IList<IElement> currentContent) {
      IDictionary<string, string> attributes = tag.Attributes;

      string src;
      if (!attributes.TryGetValue(HTML.Attribute.SRC, out src))
        return new List<IElement>(1);

      if (string.IsNullOrEmpty(src))
        return new List<IElement>(1);

      // Base64 Image tag
      if (src.StartsWith("data:image/", StringComparison.InvariantCultureIgnoreCase)) {
        var base64Data = src.Substring(src.IndexOf(",") + 1);
        var imagedata = Convert.FromBase64String(base64Data);
        var image = iTextSharp.text.Image.GetInstance(imagedata);

        var list = new List<IElement>();
        var htmlPipelineContext = GetHtmlPipelineContext(ctx);
        list.Add(GetCssAppliers()
          .Apply(new Chunk((iTextSharp.text.Image)GetCssAppliers()
          .Apply(image, tag, htmlPipelineContext), 0, 0, true), tag, htmlPipelineContext));
        return list;

        // Non base64 Image tag
      } else {
        return base.End(ctx, tag, currentContent);
      }
    }
  }

  /*
   * Tag input text (experimental)
   */
  public class CustomInputTagProcessor : iTextSharp.tool.xml.html.Span {
    public override IList<IElement> End(IWorkerContext ctx, Tag tag, IList<IElement> currentContent) {
      IDictionary<string, string> attributes = tag.Attributes;

      string type;
      if (!attributes.TryGetValue(HTML.Attribute.TYPE, out type))
        return new List<IElement>(1);
      if (!type.ToLower().Equals("text"))
        return new List<IElement>(1);

      Font fonte = Funcoes.obtemFonte(tag.CSS);
      string value = attributes["value"];
      var chunk = new Chunk(value, fonte);
      var phrase = new Phrase(chunk);

      var list = new List<IElement>();
      var htmlPipelineContext = GetHtmlPipelineContext(ctx);
      try {
        list.Add(GetCssAppliers().Apply(chunk, tag, htmlPipelineContext));
      } catch (NoCustomContextException e) {
        throw new Exception("NoCustomContextException (" + e + ").");
      }

      return list;
    }
  }

  /*
    * Tag select (experimental)
    */
  public class CustomSelectTagProcessor : iTextSharp.tool.xml.html.Span {
    public override IList<IElement> End(IWorkerContext ctx, Tag tag, IList<IElement> currentContent) {
      IDictionary<string, string> attributes = tag.Attributes;

      Font fonte = Funcoes.obtemFonte(tag.CSS);
      string value = "";
      foreach (Tag option in tag.Children) {
        if (option.Attributes.Keys.Contains("selected")) {
          if (option.Attributes.Keys.Contains("data-conteudo"))
            value = option.Attributes["data-conteudo"].Trim();
        }
      }
      var chunk = new Chunk(value, fonte);
      var phrase = new Phrase(chunk);

      var list = new List<IElement>();
      var htmlPipelineContext = GetHtmlPipelineContext(ctx);
      try {
        list.Add(GetCssAppliers().Apply(chunk, tag, htmlPipelineContext));
      } catch (NoCustomContextException e) {
        throw new Exception("NoCustomContextException (" + e + ").");
      }

      return list;
    }
  }

  public class CustomPipeline: AbstractPipeline {
    private int indent = -1;

    public CustomPipeline(IPipeline next): base(next) {
    }

    /* (non-Javadoc)
     * @see com.itextpdf.tool.xml.pipeline.AbstractPipeline#open(com.itextpdf.tool.xml.WorkerContext, com.itextpdf.tool.xml.Tag, com.itextpdf.tool.xml.ProcessObject)
     */
    public override IPipeline Open(IWorkerContext context, Tag t, ProcessObject po) {
      indent++;
      for (int i = 0; i < indent; i++)
        System.Diagnostics.Debug.Write("\t");
      System.Diagnostics.Debug.WriteLine("<" + t.Name + ">");

      return base.Open(context, t, po);
    }

    /* (non-Javadoc)
     * @see com.itextpdf.tool.xml.pipeline.AbstractPipeline#close(com.itextpdf.tool.xml.WorkerContext, com.itextpdf.tool.xml.Tag, com.itextpdf.tool.xml.ProcessObject)
     */
    public override IPipeline Close(IWorkerContext context, Tag t, ProcessObject po) {
      for (int i = 0; i < indent; i++)
        System.Diagnostics.Debug.Write("\t");
      System.Diagnostics.Debug.WriteLine("</" + t.Name + ">");
      indent--;

      return base.Close(context, t, po);
    }
  }

  /*
  // Without @WrapToTest annotation, because this test only illustrated custom element handler
  public class D01_CustomElementHandler {
    public static String SRC = "resources/xml/walden.html";

    public static void itera() {
      SampleHandler sh = new SampleHandler() {
        public void Add(Object w) {
          if (w.GetType() == typeof(WritableElement)) {
            List<Element> elements = (List<Element>)((WritableElement)w).Elements();
            foreach (Element element in elements) {
              System.Diagnostics.Debug.WriteLine(element.GetType());
            }
          }
        }
      };
      XMLWorkerHelper.GetInstance().ParseXHtml(sh, new FileInputStream(SRC), null);
    }
  }
  */

  public class SampleHandler : IElementHandler {

    // Generic list of elements
    public List<IElement> elements = new List<IElement>();

    // Add the supplied item to the list
    public void Add(IWritable w) {
      if (w is WritableElement) {
        elements.AddRange(((WritableElement)w).Elements());
      }
    }
  }

  //
  public static class Funcoes {
    public static Font obtemFonte(IDictionary<string, string> estilo) {

      FontFamily familia = FontFamily.TIMES_ROMAN;
      if (estilo.ContainsKey("font-family")) {
        if (estilo["font-family"].ToLower().IndexOf("courier") > -1 ||
          estilo["font-family"].ToLower().IndexOf("lucida") > -1)
          familia = FontFamily.COURIER;
        if (estilo["font-family"].ToString().ToLower().IndexOf("helvetica") > -1 ||
          estilo["font-family"].ToLower().IndexOf("sans") > -1 ||
          estilo["font-family"].ToLower().IndexOf("arial") > -1 ||
          estilo["font-family"].ToLower().IndexOf("verdana") > -1 ||
          estilo["font-family"].ToLower().IndexOf("tahoma") > -1)
          familia = FontFamily.HELVETICA;
      }

      float tamanho = 12.0f;
      if (estilo.ContainsKey("font-size")) {
        float.TryParse(estilo["font-size"].Replace("px", "").Replace("pt", ""), out tamanho);
      }

      BaseColor cor = BaseColor.BLACK;
      if (estilo.ContainsKey("color")) {
        if (estilo["color"].ToLower().Equals("blue"))
          cor = BaseColor.BLUE;
        if (estilo["color"].ToLower().Equals("red"))
          cor = BaseColor.RED;
        if (estilo["color"].ToLower().Equals("green"))
          cor = BaseColor.GREEN;
      }

      int tipo = Font.NORMAL;
      if (estilo.ContainsKey("font-weight")) {
        if (estilo["font-weight"].ToLower().IndexOf("bold") > -1)
          tipo = Font.BOLD;
      }

      return new Font(familia, tamanho, tipo, cor);
    }
  }
}
