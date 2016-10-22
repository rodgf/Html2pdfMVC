using System;
using System.IO;
using System.Text;
using System.Web.Mvc;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

namespace Html2pdfMVC.Controllers {
  public class GeraPDF: ActionResult {
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
        (new FileContentResult(GeraDocumento(cc), "application/pdf")).ExecuteResult(cc);
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
              XMLWorkerHelper.GetInstance().ParseXHtml(pw, doc, sr);
            } catch (Exception ee) {
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
}
