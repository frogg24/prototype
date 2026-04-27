using BusinessLogic;
using DataModels;
using DataModels.ProjectModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ProjectLogic _projectLogic;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(ProjectLogic projectLogic, ILogger<ProjectController> logger)
        {
            _projectLogic = projectLogic;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectModel model)
        {
            _logger.LogInformation($"Create project request received, userID={model.UserId}, title={model.Title}");
            try
            {
                model.CreatedAt = DateTime.UtcNow;

                var result = await _projectLogic.Create(model);

                if (result)
                {
                    _logger.LogInformation($"Create project success, userID={model.UserId}, title={model.Title}");
                    return Ok(new { message = "Проект успешно создан" });
                }

                _logger.LogWarning($"Create project failed, userID={model.UserId}, title={model.Title}");
                return BadRequest(new { message = "Ошибка при создании проекта" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create project failed, validation error, userID={model.UserId}, title={model.Title}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create project failed, conflict, userID={model.UserId}, title={model.Title}");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create project failed, unexpected error, userID={model.UserId}, title={model.Title}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            _logger.LogInformation($"Get projects by user request received, userID={userId}");
            try
            {
                var projects = await _projectLogic.ReadList(new ProjectSearchModel
                {
                    UserId = userId
                });

                _logger.LogInformation($"Get projects by user success, userID={userId}");
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get projects by user failed, unexpected error, userID={userId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation($"Get project by ID request received, projectID={id}");
            try
            {
                var project = await _projectLogic.ReadElement(new ProjectSearchModel
                {
                    Id = id
                });

                if (project == null)
                {
                    _logger.LogWarning($"Get project by ID failed, project not found, projectID={id}");
                    return NotFound(new { message = "Проект не найден" });
                }

                _logger.LogInformation($"Get project by ID success, projectID={id}");
                return Ok(project);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get project by ID failed, unexpected error, projectID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectModel request)
        {
            _logger.LogInformation($"Update project request received, projectID={id}");
            try
            {
                var existingProject = await _projectLogic.ReadElement(new ProjectSearchModel
                {
                    Id = id
                });

                if (existingProject == null)
                {
                    _logger.LogWarning($"Update project failed, project not found, projectID={id}");
                    return NotFound(new { message = "Проект не найден" });
                }

                existingProject.Title = request.Title ?? existingProject.Title;
                existingProject.UpdatedAt = DateTime.UtcNow;

                var result = await _projectLogic.Update(existingProject);

                if (result)
                {
                    _logger.LogInformation($"Update project success, projectID={id}");
                    return Ok(new { message = "Проект обновлён" });
                }

                _logger.LogWarning($"Update project failed, projectID={id}");
                return BadRequest(new { message = "Ошибка при обновлении проекта" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update project failed, validation error, projectID={id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update project failed, conflict, projectID={id}");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update project failed, unexpected error, projectID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Delete project request received, projectID={id}");
            try
            {
                var result = await _projectLogic.Delete(id);

                if (!result)
                {
                    _logger.LogWarning($"Delete project failed, project not found, projectID={id}");
                    return NotFound(new { message = "Проект не найден" });
                }

                _logger.LogInformation($"Delete project success, projectID={id}");
                return Ok(new { message = "Проект удалён" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete project failed, unexpected error, projectID={id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}