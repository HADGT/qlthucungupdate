using Microsoft.AspNetCore.Mvc;

namespace qlthucung.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
