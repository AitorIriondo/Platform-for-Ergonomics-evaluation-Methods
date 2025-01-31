using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PEM.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xsens;

namespace PEM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileUploadController : ControllerBase
{

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string parser)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }
        try
        {
            string filePath = Path.Combine(Paths.Uploads, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            if(parser == "XsensManikin") {
                //Ugly but works for now
                XsensManikin.Message msg = new XsensManikin.Message() { parser=parser, file=filePath};
                ManikinManager.ParseMessage(JsonConvert.SerializeObject(msg));
            }
            return Ok(new { message = "File uploaded successfully", fileName = file.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
