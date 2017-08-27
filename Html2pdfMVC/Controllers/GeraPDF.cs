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

using static Html2pdfMVC.Models.NGS;

namespace Html2pdfMVC.Controllers {
  public class GeraPDF : ActionResult {
    string[] css;

    public GeraPDF(object modelo) {
      this.Modelo = modelo;
    }
    public GeraPDF(string nomeView, object modelo) {
      NomeView = nomeView;
      this.Modelo = modelo;
    }
    public GeraPDF(string nomeView, object modelo, string[] css) {
      NomeView = nomeView;
      this.Modelo = modelo;
      this.css = css;
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
    public override void ExecuteResult(ControllerContext contextoC) {
      if (NomeView == null) {
        NomeView = contextoC.RouteData.GetRequiredString("action");
      }

      // Define destino do buffer conforme tipo de saída: tela ou download
      contextoC.Controller.ViewData.Model = Modelo;
      if (contextoC.HttpContext.Request.Form["saida"] != null &&
          contextoC.HttpContext.Request.Form["saida"].ToLower().Equals("html")) {
        RenderizaHtml(contextoC);
      } else {

        // Arquivo para download
        if (Download != null)
          contextoC.HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + Download);
        byte[] buff = GeraDocumento(contextoC);
        if (buff != null)
          (new FileContentResult(buff, "application/pdf")).ExecuteResult(contextoC);
      }
    }

    // Compõe o documento PDF
    public byte[] GeraDocumento(ControllerContext contextoC) {
      StyleAttrCSSResolver cssResolver;

      contextoC.Controller.ViewData.Model = Modelo;

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
          using (StringReader sr = new StringReader(preparaHtml(StringHtml(contextoC)))) {

            // Versão VahidN - tags customizadas (ver classe abaixo)
            var tagProcessors = (DefaultTagProcessorFactory)Tags.GetHtmlTagProcessorFactory();
            tagProcessors.RemoveProcessor(HTML.Tag.IMG);
            tagProcessors.AddProcessor(HTML.Tag.IMG, new CustomImageTagProcessor());              // Tag IMG

            // Campos de formuários
            tagProcessors.RemoveProcessor(HTML.Tag.INPUT);
            tagProcessors.AddProcessor(HTML.Tag.INPUT, new CustomInputTagProcessor());            // Tag INPUT (experimental)
            tagProcessors.AddProcessor(HTML.Tag.SELECT, new CustomSelectTagProcessor());          // Tag SELECT (experimental)

            // Folhas de estilo
            CssFilesImpl cssFiles = new CssFilesImpl();
            cssFiles.Add(XMLWorkerHelper.GetInstance().GetDefaultCSS());
            cssResolver = new StyleAttrCSSResolver(cssFiles);
            cssResolver.AddCss(@"code { padding: 2px 4px; }", "utf-8", true);

            var charset = Encoding.UTF8;
            var hpc = new HtmlPipelineContext(new CssAppliersImpl(new XMLWorkerFontProvider()));
            hpc.SetAcceptUnknown(true).AutoBookmark(true).SetTagFactory(tagProcessors);   // registra os processadores de tags customizadas

            // Prepara o parser
            var htmlPipeline = new HtmlPipeline(hpc, new PdfWriterPipeline(doc, pw));
            var pipeline = new CssResolverPipeline(cssResolver, htmlPipeline);

            if (this.css != null) {
              foreach (string cf in this.css)
                cssResolver.AddCssFile(cf, true);
            }

            var worker = new XMLWorker(pipeline, true);
            var xmlParser = new XMLParser(true, worker, charset);
            try {
              xmlParser.Parse(sr);

              //XMLWorkerHelper.GetInstance().ParseXHtml(pw, doc, sr);                    // versão simplificada, dispensa o código acima
            } catch (Exception ee) {
              contextoC.HttpContext.Session["Erro"] = ee;
              try {
                doc.Dispose();
              } catch (Exception) { }
              contextoC.Controller.TempData["Erro"] = ee;
              NomeView = "Erro";
              RenderizaHtml(contextoC);
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

      // Obtém html de origem
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
      return stResult;
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

        Font fonte = obtemFonte(tag.CSS);
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
     * Tag textarea text (experimental)
     */
    public class CustomTextareaTagProcessor : iTextSharp.tool.xml.html.Span {
      public override IList<IElement> End(IWorkerContext ctx, Tag tag, IList<IElement> currentContent) {
        IDictionary<string, string> attributes = tag.Attributes;

        Font fonte = obtemFonte(tag.CSS);
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

        Font fonte = obtemFonte(tag.CSS);
        string value = "";
        foreach (Tag option in tag.Children) {
          if (option.Attributes.Keys.Contains("selected")) {
            if (option.Attributes.Keys.Contains("data-conteudo"))   // esta tag foi acrescentada em preparaHtml()
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
  }
}
