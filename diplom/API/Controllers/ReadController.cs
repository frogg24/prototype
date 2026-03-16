using BusinessLogic;
using Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadController
    {
        private readonly ReadLogic _readLogic;

        public ReadController(ReadLogic readLogic)
        {
            _readLogic = readLogic;
        }

        //[HttpPost("upload")]
        //public async Task<IActionResult> Upload(IFormFile file)
        //{
        //    var result = await _readService.UploadReadAsync(file, userId);
        //    return Ok(result);
        //}
    }
}
