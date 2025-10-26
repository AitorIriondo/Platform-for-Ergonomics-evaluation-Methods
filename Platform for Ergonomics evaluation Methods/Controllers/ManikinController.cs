using Microsoft.AspNetCore.Mvc;

namespace PEM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManikinController : ControllerBase
    {
        [HttpGet("list")]
        public IActionResult List()
        {
            var list = ManikinManager.List().Select(x => new { id = x.id, name = x.name }).ToList();
            return Ok(new { activeId = ManikinManager.ActiveManikinId, items = list });
        }

        [HttpPost("select")]
        public IActionResult Select([FromBody] SelectRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Id)) return BadRequest("Missing id");
            if (!ManikinManager.Select(req.Id)) return NotFound("Unknown manikin id");
            return Ok(new { activeId = ManikinManager.ActiveManikinId });
        }

        [HttpDelete("{id}")]
        public IActionResult Remove(string id)
        {
            if (!ManikinManager.Remove(id)) return NotFound("Unknown manikin id");
            return Ok(new { activeId = ManikinManager.ActiveManikinId });
        }

        public class SelectRequest { public string Id { get; set; } = ""; }
    }
}
