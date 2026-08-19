using Microsoft.AspNetCore.Mvc;

namespace AuthenticationDemo.Controllers {
    public class AccountController : Controller {
        public IActionResult Login() {
            return View();
        }
    }
}
