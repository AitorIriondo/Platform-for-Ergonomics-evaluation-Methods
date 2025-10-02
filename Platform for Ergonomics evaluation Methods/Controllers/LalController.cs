using Microsoft.AspNetCore.Mvc;
using PEM.Models;
using System;

namespace PEM.Controllers
{
    // MVC controller (serves a view) + keeps an API endpoint for JS
    public class LalController : Controller
    {
        // GET /Lal  -> returns Views/Lal/Index.cshtml
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Computes LAL percentiles for the currently loaded manikin.
        /// Returns: metric -> { p10, p50, p90 }
        /// Kept under /api/lal/percentiles so your JS can call it.
        /// </summary>
        [HttpGet("/api/lal/percentiles")]
        public IActionResult GetPercentiles([FromQuery] float dt = 0.1f)
        {
            var manikin = ManikinManager.loadedManikin;
            if (manikin == null)
                return BadRequest("No manikin loaded. Import an IMMA/Xsens manikin first.");

            try
            {
                var lal = new Lal();
                var result = lal.Compute(manikin, dt);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
