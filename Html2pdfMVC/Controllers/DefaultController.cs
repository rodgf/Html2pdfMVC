﻿using Html2pdfMVC.Helpers;
using Html2pdfMVC.Models;
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
      string stImagem = Server.MapPath("/images/CEASA.jpg");
      PDFComImagem modelo = new PDFComImagem();
      modelo.Imagens.Add("Img1", img2base64(stImagem));
      modelo.ID = (new Random()).Next(100);

      return View(modelo);
    }

    // Gera PDF a partir de formulário
    public ActionResult Form() {
      FormV modelo = new FormV();

      if (Request.Form["destino"] == "PDF")
        return new GeraPDF("Form", modelo);
      else
        return View(modelo);
    }

    // Gera PDF a partir da view
    [HttpPost]
    public ActionResult gerarPDF() {
      object modelo = 0;

      if (Request.Form["id"] != null)
        modelo = int.Parse(Request.Form["id"].ToString());

      string[] css = new[] { Server.MapPath(Url.Content("~/Content/Site.css")) };

      return new GeraPDF("Index", modelo, css);
    }

    // Gera PDF com imagem a partir da view
    [HttpPost]
    public ActionResult gerarPDFImagem() {
      string stImagem = Server.MapPath("/images/CEASA.jpg");
      PDFComImagem modelo = new PDFComImagem();
      modelo.Imagens.Add("Img1", img2base64(stImagem));
      if (Request.Form["id"] != null)
        modelo.ID = int.Parse(Request.Form["id"].ToString());

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

    // Converte arquivo para base64
    string img2base64(string arquivo) {
      string stResult = "";

      try {
        Byte[] bytes = System.IO.File.ReadAllBytes(arquivo);
        stResult = Convert.ToBase64String(bytes);
      } catch (Exception ee) {
        System.Diagnostics.Debug.WriteLine("Falha ao converter arquivo para base64 (" + ee + ").");
      }
      return stResult;
    }
  }
}
