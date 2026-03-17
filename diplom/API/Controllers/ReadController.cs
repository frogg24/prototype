using BusinessLogic;
using DataModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        [AllowAnonymous]
        [HttpPost("project/{projectId:int}/upload")]
        public async Task<IActionResult> Upload(int projectId, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
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

                var saved = await _readLogic.UploadReadsAsync(projectId, models);
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

        [HttpGet("project/{projectId:int}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            try
            {
                var reads = await _readLogic.GetProjectReadsAsync(projectId);
                return Ok(reads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var read = await _readLogic.ReadElement(new ReadSearchModel { Id = id });

                if (read == null)
                {
                    return NotFound(new { message = "Рид не найден" });
                }

                return Ok(read);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _readLogic.Delete(id);

                if (!result)
                {
                    return NotFound(new { message = "Рид не найден" });
                }

                return Ok(new { message = "Рид удалён" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
