using BusinessLogic;
using DataModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadController:ControllerBase
    {
        private readonly ReadLogic _readLogic;

        public ReadController(ReadLogic readLogic)
        {
            _readLogic = readLogic;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromQuery] int userId, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (files.Count == 0)
                {
                    return BadRequest(new { message = "Не выбраны файлы для загрузки" });
                }

                var models = new List<UploadReadFileModel>();
                foreach (var file in files.Where(f => f.Length > 0))
                {
                    await using var memory = new MemoryStream();
                    await file.CopyToAsync(memory);

                    models.Add(new UploadReadFileModel
                    {
                        FileName = file.FileName,
                        Content = memory.ToArray(),
                    });
                }

                var saved = await _readLogic.UploadReadsAsync(userId, models);
                return Ok(saved);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var reads = await _readLogic.GetUserReadsAsync(userId);
                return Ok(reads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
