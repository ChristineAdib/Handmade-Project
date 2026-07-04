using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        // Redirect root URL to the Angular frontend
        var frontendUrl = _configuration["FrontendUrl"] ?? "https://handauraa.runasp.net";
        return Redirect(frontendUrl);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
