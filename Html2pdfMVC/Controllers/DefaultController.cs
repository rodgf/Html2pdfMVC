using System;
using System.Web.Mvc;

namespace Html2pdfMVC.Controllers {
  public class DefaultController : Controller {

    // GET: Default
    public ActionResult Index() {
      int inID = (new Random()).Next(100);

      ViewData["ID"] = inID;
      return View();
    }
  }
}
