# Html2pdfMVC
Gera PDF a partir de página HTML Razor

Este projeto foi inspirado no projeto MvcRazorToPdf de Andrew Hutchinson ( [https://github.com/andyhutch77/MvcRazorToPdf] ), como uma simplificação e acrescentando tratamento de erros.

Converte uma página razor/html para pdf no navegador usando iText XML Worker (iTextXmlWorker), disponibilizado nas últimas versões da bibliotexa iText/iTextSarp.

É interessante notar que nem todas as tags html e folhas estilos são processadas. Particularmente, os CSS importados não são processados.

O html, por outro lado, deve ser composto como um XML válido, ou seja, todas as tags fechadas, etc.

**Utilização em um projeto existente:**

- Acrescente as bibliotecas itextsharp e itextsharp.xmlworker ao seu projeto;
- Inclua a classe GeraPDF.cs no projeto
- Faça a chamada a partir da Controller no formato 'return new GeraPDF("NomeDaView", modelo);' - a classe estende ActionResult e despeja o PDF na saída do contexto http
- Outras modificações são permitidas, inclusive download direto do PDF (veja exemplo no próprio código do projeto)

A view em questão será a própria view usada para gerar o PDF, não é necessário criar outra view. Você pode incluir um botão na mesma view fazendo a chamada à ação que gerará o PDF.

**Projeto de exemplo**

( https://github.com/rodgf/Html2pdfMVC )

**Mais informações**

[iTextXmlWorker docs] ( http://demo.itextsupport.com/xmlworker/itextdoc/flatsite.html )

[Demo] ( http://demo.itextsupport.com/xmlworker/ )

[CSS suportados] ( http://demo.itextsupport.com/xmlworker/itextdoc/CSS-conformance-list.htm )
