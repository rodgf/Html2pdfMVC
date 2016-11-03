using iTextSharp.text;
using System;
using System.Web.Mvc;

namespace Html2pdfMVC.Controllers {
  public class DefaultController : Controller {

    // GET: Default
    public ActionResult Index() {
      object modelo = (new Random()).Next(100);

      return View(modelo);
    }

    // Erro
    public ActionResult Erro() {
      return View();
    }

    // HTML com imagem
    public ActionResult ComImagem() {
      object modelo = (new Random()).Next(100);

      return View(modelo);
    }

    // Gera PDF a partir da view
    [HttpPost]
    public ActionResult gerarPDF() {
      object modelo = 0;

      if (Request.Form["id"] != null)
        modelo = int.Parse(Request.Form["id"].ToString());

      return new GeraPDF("Index", modelo);
    }

    // Gera PDF com imagem a partir da view
    [HttpPost]
    public ActionResult gerarPDFImagem() {
      object modelo = 0;

      if (Request.Form["id"] != null)
        modelo = int.Parse(Request.Form["id"].ToString());

      return new GeraPDF("ComImagem", modelo, (writer, document) => {
        document.SetPageSize(new Rectangle(850f, 600f, 90));
      });
    }

    // Gera PDF com alterações direcionando a download
    [HttpPost]
    public ActionResult geraPDFAlt() {
      object modelo = 0;

      if (Request.Form["id"] != null)
        modelo = int.Parse(Request.Form["id"].ToString());

      return new GeraPDF(modelo, (writer, document) => {
        document.SetPageSize(new Rectangle(500f, 500f, 90));
        document.NewPage();
      }) {
        Download = "Saida.pdf"
      };
    }
  }
}
