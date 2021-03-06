# Html2pdfMVC
Gera PDF a partir de página HTML Razor

Este projeto foi inspirado no projeto MvcRazorToPdf de [Andrew Hutchinson](https://github.com/andyhutch77/MvcRazorToPdf), como uma simplificação e acrescentando tratamento de erros.

Converte uma página razor/html para pdf no navegador usando iText XML Worker (iTextXmlWorker), disponibilizado nas últimas versões da bibliotexa iText/iTextSarp.

O html deve ser composto como um XML válido, ou seja, todas as tags fechadas, etc. (*)

(*) A versão 2.0 traz um filtro de validação que evita erros de html mal formatado.

**Utilização em um projeto existente:**

- Acrescente as bibliotecas itextsharp e itextsharp.xmlworker ao seu projeto;
- Inclua a classe GeraPDF.cs no projeto;
- Faça a chamada a partir da Controller no formato 'return new GeraPDF("NomeDaView", modelo);' - a classe estende ActionResult e despeja o PDF na saída do contexto http;
- Outras modificações são permitidas, inclusive download direto do PDF (veja exemplo no próprio código do projeto).

A view em questão será a própria view usada para gerar o PDF, não é necessário criar outra view. Você pode incluir um botão na mesma view fazendo a chamada à ação que gerará o PDF.

**HTML com imagens**

Graças ao artigo de VahidN em [StackOverflow](http://stackoverflow.com/questions/19389999/can-itextsharp-xmlworker-render-embedded-images) é possível incluirem-se imagens no PDF a partir de tags &lt;img&gt; usando conversão para base64. Veja exemplo no projeto.

**Projeto de exemplo**

( https://github.com/rodgf/Html2pdfMVC )

**Histórico de versões**

- Versão 2.0:
	- Acrescentada funcionalidade para tratamento de tags &lt;input type="text"&gt; e &lt;select&gt; (experimental)
	- conta com um filtro de validação baseado em Html Agility Pack, para evitar erros de html mal formatado
	
**Créditos**

- Andrew Hutchinson - [MvcRazorToPdf](https://github.com/andyhutch77/MvcRazorToPdf)
- StackOverflow - [Can itextsharp.xmlworker render embedded images?](http://stackoverflow.com/questions/19389999/can-itextsharp-xmlworker-render-embedded-images)

**Mais informações**

[iTextXmlWorker docs] ( http://demo.itextsupport.com/xmlworker/itextdoc/flatsite.html )

[Demo] ( http://demo.itextsupport.com/xmlworker/ )

[CSS suportados] ( http://demo.itextsupport.com/xmlworker/itextdoc/CSS-conformance-list.htm )

[Can itextsharp.xmlworker render embedded images?] ( http://stackoverflow.com/questions/19389999/can-itextsharp-xmlworker-render-embedded-images )
