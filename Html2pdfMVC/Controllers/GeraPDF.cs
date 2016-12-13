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
            try {

              // Versão VahidN (ver classe abaixo)
              var tagProcessors = (DefaultTagProcessorFactory)Tags.GetHtmlTagProcessorFactory();
              tagProcessors.RemoveProcessor(HTML.Tag.IMG); // remove the default processor
              tagProcessors.AddProcessor(HTML.Tag.IMG, new CustomImageTagProcessor());              // use our new processor

              // Campo texto de formuários
              tagProcessors.AddProcessor(HTML.Tag.INPUT, new CustomInputTagProcessor());            // (experimental)

              CssFilesImpl cssFiles = new CssFilesImpl();
              cssFiles.Add(XMLWorkerHelper.GetInstance().GetDefaultCSS());
              var cssResolver = new StyleAttrCSSResolver(cssFiles);
              cssResolver.AddCss(@"code { padding: 2px 4px; }", "utf-8", true);

              var charset = Encoding.UTF8;
              var hpc = new HtmlPipelineContext(new CssAppliersImpl(new XMLWorkerFontProvider()));
              hpc.SetAcceptUnknown(true).AutoBookmark(true).SetTagFactory(tagProcessors);           // inject the tagProcessors

              var htmlPipeline = new HtmlPipeline(hpc, new PdfWriterPipeline(doc, pw));
              var pipeline = new CssResolverPipeline(cssResolver, htmlPipeline);
              var worker = new XMLWorker(pipeline, true);
              var xmlParser = new XMLParser(true, worker, charset);
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
      return sb.ToString();
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
   * Tag de input text (experimental
   * 
   */
  public class CustomInputTagProcessor : iTextSharp.tool.xml.html.Span {
    public override IList<IElement> End(IWorkerContext ctx, Tag tag, IList<IElement> currentContent) {
      IDictionary<string, string> attributes = tag.Attributes;
      Font fontNormal = new Font(FontFamily.TIMES_ROMAN, 12.0f, Font.NORMAL, BaseColor.BLACK);

      string type;
      if (!attributes.TryGetValue(HTML.Attribute.TYPE, out type))
        return new List<IElement>(1);
      if (!type.ToLower().Equals("text"))
        return new List<IElement>(1);
      string value = attributes["value"];

      var chunk = new Chunk(value, fontNormal);
      var phrase = new Phrase(chunk);

      var list = new List<IElement>();
      var htmlPipelineContext = GetHtmlPipelineContext(ctx);
      /*
      IElement elemento = GetCssAppliers()
        .Apply(phrase, tag, htmlPipelineContext);
      list.Add(elemento);
      */

      try {
        list.Add(GetCssAppliers().Apply(chunk, tag, htmlPipelineContext));

      } catch (NoCustomContextException e) {
        throw new Exception("NoCustomContextException (" + e);
      }

      return list;
    }
  }
}
