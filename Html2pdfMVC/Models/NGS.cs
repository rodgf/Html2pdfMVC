using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using HtmlAgilityPack;

using iTextSharp.text;

using static iTextSharp.text.Font;

namespace Html2pdfMVC.Models {
  public class NGS {

  // Prepara Html
  public static string preparaHtml(string html) {
    string stResult = html;

    // Filtra documento
    StringBuilder sb = new StringBuilder();
    StringWriter sw = new StringWriter(sb);
    HtmlDocument had = new HtmlDocument();
    
    // Comportamento de tags específicas
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

    // Configurações
    had.OptionOutputAsXml = false;
    had.OptionCheckSyntax = true;
    had.OptionFixNestedTags = true;
    had.OptionAutoCloseOnEnd = false;
    had.OptionWriteEmptyNodes = true;

    // Tratamento
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

  // Compõe fonte com base em folha de estilo
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
