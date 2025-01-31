using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PEM.Models;
using PEM.Services;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Nodes;

namespace PEM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MessageStorageService _messageStorageService;
        private string prevJsonMessage = "";

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

        [HttpGet]
        public IActionResult GetLoadedManikinInfo()
        {
            string type = "None";
            if (ManikinManager.loadedManikin != null)
            {
                type = ManikinManager.loadedManikin.GetType().ToString();
                string description = ManikinManager.loadedManikin.GetDescriptiveName();
                return Json(new { type, description });
            }
            return Json(new { type });
        }

        [HttpGet]
        public IActionResult LoadLastManikin()
        {
            string res = ManikinManager.LoadLast() ? "OK" : "FAIL";
            return Json(new { res });
        }
        [HttpPost]
        public IActionResult LoadTestData(string msg)
        {
            string res = ManikinManager.ParseMessage(msg) ? "OK" : "FAIL";
            return Json(new { res });
        }

    }
}