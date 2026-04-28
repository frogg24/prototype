using BusinessLogic;
using DataModels.AssemblyModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssemblyController : ControllerBase
    {
        private readonly AssemblyLogic _assemblyLogic;
        private readonly ILogger<AssemblyController> _logger;

        public AssemblyController(AssemblyLogic assemblyLogic, ILogger<AssemblyController> logger)
        {
            _assemblyLogic = assemblyLogic;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AssemblyModel model)
        {
            _logger.LogInformation($"Create assembly request received, projectID={model.ProjectId}");
            try
            {
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                var result = await _assemblyLogic.Create(model);

                if (result)
                {
                    _logger.LogInformation($"Create assembly success, projectID={model.ProjectId}");
                    return Ok(new { message = "Сборка успешно создана" });
                }

                _logger.LogWarning($"Create assembly failed, projectID={model.ProjectId}");
                return BadRequest(new { message = "Ошибка при создании сборки" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create assembly failed, validation error, projectID={model.ProjectId}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create assembly failed, conflict, projectID={model.ProjectId}");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create assembly failed, unexpected error, projectID={model.ProjectId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("project/{projectId:long}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            _logger.LogInformation($"Get assemblies by project request received, projectID={projectId}");
            try
            {
                var assemblies = await _assemblyLogic.ReadList(new AssemblySearchModel
                {
                    ProjectId = projectId
                });

                _logger.LogInformation($"Get assemblies by project success, projectID={projectId}");
                return Ok(assemblies);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get assemblies by project failed, unexpected error, projectID={projectId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation($"Get assembly by ID request received, assemblyID={id}");
            try
            {
                var assembly = await _assemblyLogic.ReadElement(new AssemblySearchModel
                {
                    Id = id
                });

                if (assembly == null)
                {
                    _logger.LogWarning($"Get assembly by ID failed, assembly not found, assemblyID={id}");
                    return NotFound(new { message = "Сборка не найдена" });
                }

                _logger.LogInformation($"Get assembly by ID success, assemblyID={id}");
                return Ok(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get assembly by ID failed, unexpected error, assemblyID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AssemblyModel request)
        {
            _logger.LogInformation($"Update assembly request received, assemblyID={id}");
            try
            {
                var existingAssembly = await _assemblyLogic.ReadElement(new AssemblySearchModel
                {
                    Id = id
                });

                if (existingAssembly == null)
                {
                    _logger.LogWarning($"Update assembly failed, assembly not found, assemblyID={id}");
                    return NotFound(new { message = "Сборка не найдена" });
                }

                existingAssembly.ConsensusSequence = request.ConsensusSequence;
                existingAssembly.ConsensusLength = request.ConsensusLength;
                existingAssembly.UpdatedAt = DateTime.UtcNow;

                var result = await _assemblyLogic.Update(existingAssembly);

                if (result)
                {
                    _logger.LogInformation($"Update assembly success, assemblyID={id}");
                    return Ok(new { message = "Сборка обновлена" });
                }

                _logger.LogWarning($"Update assembly failed, assemblyID={id}");
                return BadRequest(new { message = "Ошибка при обновлении сборки" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update assembly failed, validation error, assemblyID={id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update assembly failed, conflict, assemblyID={id}");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update assembly failed, unexpected error, assemblyID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Delete assembly request received, assemblyID={id}");
            try
            {
                var result = await _assemblyLogic.Delete(id);

                if (!result)
                {
                    _logger.LogWarning($"Delete assembly failed, assembly not found, assemblyID={id}");
                    return NotFound(new { message = "Сборка не найдена" });
                }

                _logger.LogInformation($"Delete assembly success, assemblyID={id}");
                return Ok(new { message = "Сборка удалена" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete assembly failed, unexpected error, assemblyID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("project/{projectId:int}/run")]
        public async Task<IActionResult> RunAssembly(int projectId)
        {
            _logger.LogInformation($"Run assembly request received, projectID={projectId}");
            try
            {
                var assembly = await _assemblyLogic.MakeOLC(projectId);

                if (assembly == null)
                {
                    _logger.LogWarning($"Run assembly failed, assembly result is null, projectID={projectId}");
                    return BadRequest(new { message = "Не удалось выполнить сборку" });
                }

                _logger.LogInformation($"Run assembly success, projectID={projectId}");
                return Ok(assembly);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Run assembly failed, validation error, projectID={projectId}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Run assembly failed, conflict, projectID={projectId}");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Run assembly failed, unexpected error, projectID={projectId}");
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }
    }
}