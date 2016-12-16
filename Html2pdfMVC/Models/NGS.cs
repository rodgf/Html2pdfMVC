using System;
using System.Reflection;
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

      // Família
      string[] stHelvetica = new string[] { "helvetica", "sans", "serif", "arial", "verdana", "tahoma" };
      string[] stCourier = new string[] { "courier", "lucida", "monospace" };
      FontFamily familia = FontFamily.TIMES_ROMAN;
      if (estilo.ContainsKey("font-family")) {
        foreach (string stFonte in stCourier) {
          if (estilo["font-family"].ToLower().IndexOf(stFonte) > -1)
            familia = FontFamily.COURIER;
        }
        foreach (string stFonte in stHelvetica) {
          if (estilo["font-family"].ToString().ToLower().IndexOf(stFonte) > -1)
            familia = FontFamily.HELVETICA;
        }
      }

      // Tamanho
      float tamanho = 12.0f;
      if (estilo.ContainsKey("font-size")) {
        float.TryParse(estilo["font-size"].Replace("px", "").Replace("pt", ""), out tamanho);
      }

      // Cor
      BaseColor cor = BaseColor.BLACK;
      foreach (PropertyInfo pi in typeof(BaseColor).GetProperties()) {
        if (pi.GetType() == typeof(BaseColor)) {
          if (estilo["color"].ToLower().Equals(pi.GetValue(null).ToString()))
            cor = (BaseColor)pi.GetValue(null);
        }
      }

      // Estilo
      int tipo = Font.NORMAL;
      List<int> tipos = new List<int>();
      if (estilo.ContainsKey("font-weight")) {
        if (estilo["font-weight"].ToLower().IndexOf("bold") > -1)
          tipos.Add(Font.BOLD);
      }
      if (estilo.ContainsKey("font-style")) {
        if (estilo["font-style"].ToLower().IndexOf("italic") > -1 || estilo["font-style"].ToLower().IndexOf("oblique") > -1)
          tipos.Add(Font.ITALIC);
      }
      if (tipos.Contains(Font.ITALIC) && tipos.Contains(Font.BOLD)) {
        tipo = Font.BOLDITALIC;
      } else {
        if (tipos.Contains(Font.BOLD)) {
          tipo = Font.BOLD;
        } else {
          if (tipos.Contains(Font.ITALIC)) {
            tipo = Font.ITALIC;
          }
        }
      }

      return new Font(familia, tamanho, tipo, cor);
    }
  }
}
