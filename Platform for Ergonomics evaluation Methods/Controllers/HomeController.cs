using Microsoft.AspNetCore.Mvc;
using Platform_for_Ergonomics_evaluation_Methods.Models;
using Platform_for_Ergonomics_evaluation_Methods.Services;
using System.Diagnostics;

namespace Platform_for_Ergonomics_evaluation_Methods.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MessageStorageService _messageStorageService;

        public HomeController(ILogger<HomeController> logger, MessageStorageService messageStorageService)
        {
            _logger = logger;
            _messageStorageService = messageStorageService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // API to get the latest JSON message
        [HttpGet]
        public IActionResult GetLatestJsonMessage()
        {
            string jsonMessage = _messageStorageService.GetLatestMessage();
            return Json(new { jsonMessage });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}