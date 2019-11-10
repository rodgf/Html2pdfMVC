namespace Html2pdfMVC.Models {
  public class FormV {
    public Form form;

    public FormV() {
      this.form = new Form();
      this.form.Nome = "James Dean";
      this.form.Endereco = "Alameda das Flores, 1314";
      this.form.Telefone = "(61) 3562-5656";
      this.form.Cidade = "Brasília";
      this.form.Estado = "DF";
      this.form.Pais = "Brazil";
      this.form.Comentario = "Linha 1\nLinha 2\nLinha 3";
    }
  }
}
