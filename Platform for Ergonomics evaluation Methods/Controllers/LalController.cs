using Microsoft.AspNetCore.Mvc;
using PEM.Models;
using System;

namespace PEM.Controllers
{
    public class LalController : Controller
    {
        // GET /Lal -> returns Views/Lal/Index.cshtml
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Computes LAL percentiles for the currently loaded manikin.
        /// Returns JSON: { metric : { p10, p50, p90 } }
        /// </summary>
        [HttpGet("/api/lal/percentiles")]
        public IActionResult GetPercentiles([FromQuery] float dt = 0.1f)
        {
            var manikin = ManikinManager.ActiveManikin;
            if (manikin == null)
                // ✅ wrap in object so client gets valid JSON
                return BadRequest(new { error = "No manikin loaded. Import an IMMA/Xsens manikin first." });

            try
            {
                var lal = new Lal();
                var result = lal.Compute(manikin);
                return Ok(result); // ✅ JSON
            }
            catch (Exception ex)
            {
                // ✅ include message + stack for debugging
                return BadRequest(new
                {
                    error = ex.Message,
                    details = ex.GetType().FullName,
                    stack = ex.StackTrace
                });
            }
        }
    }
}
