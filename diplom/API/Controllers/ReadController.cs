using BusinessLogic;
using DataModels.ReadModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadController : ControllerBase
    {
        private readonly ReadLogic _readLogic;
        private readonly ILogger<ReadController> _logger;

        public ReadController(ReadLogic readLogic, ILogger<ReadController> logger)
        {
            _readLogic = readLogic;
            _logger=logger;
        }

        [HttpPost("project/{projectId:int}/upload")]
        public async Task<IActionResult> Upload(int projectId, [FromForm] List<IFormFile> files)
        {
            _logger.LogInformation($"Upload reads request received, projectID={projectId}");
            try
            {
                if (files == null || files.Count == 0)
                {
                    _logger.LogWarning($"Upload failed, there are no files, projectID={projectId}");
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
                _logger.LogInformation($"Reads uploaded, projectID={projectId}");
                return Ok(saved);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Upload failed, validation error, projectID={projectId}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning($"Upload failed, wrong data format, projectID={projectId}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Upload failed, unexpected error, projectID={projectId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("project/{projectId:int}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            _logger.LogInformation($"Get project reads request received, projectID={projectId}");
            try
            {
                var reads = await _readLogic.GetProjectReadsAsync(projectId);
                _logger.LogInformation($"Get project reads success, projectID={projectId}");
                return Ok(reads);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get project reads failed, unexpected error, projectID={projectId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation($"Get read by ID request received, readID={id}");
            try
            {
                var read = await _readLogic.ReadElement(new ReadSearchModel { Id = id });

                if (read == null)
                {
                    _logger.LogWarning($"Get read by ID failed, read not found, readID={id}");
                    return NotFound(new { message = "Рид не найден" });
                }

                _logger.LogInformation($"Get read by ID success, readID={id}");
                return Ok(read);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get read by ID failed, unexpected error, readID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Delete read request received, readID={id}");
            try
            {
                var result = await _readLogic.Delete(id);

                if (!result)
                {
                    _logger.LogWarning($"Delete read failed, read not found, readID={id}");
                    return NotFound(new { message = "Рид не найден" });
                }

                _logger.LogInformation($"Delete read success, readID={id}");
                return Ok(new { message = "Рид удалён" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete read failed, unexpected error, readID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}