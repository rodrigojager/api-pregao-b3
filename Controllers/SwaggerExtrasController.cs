using Microsoft.AspNetCore.Mvc;

namespace TechChallenge.Controllers
{

    public class SwaggerExtrasController : Controller
    {
        [HttpGet("/swagger/menu")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Menu()
        => PartialView("~/Views/Shared/_sidebar_api.cshtml");
    }
}
