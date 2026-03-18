using BusinessLogic;
using DataModels;
using DataModels.ProjectModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController: ControllerBase
    {
        private readonly ProjectLogic _projectLogic;

        public ProjectController(ProjectLogic projectLogic)
        {
            _projectLogic = projectLogic;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectModel model)
        {
            try
            {
                model.CreatedAt = DateTime.UtcNow;

                var result = await _projectLogic.Create(model);

                if (result)
                {
                    return Ok(new { message = "Проект успешно создан" });
                }

                return BadRequest(new { message = "Ошибка при создании проекта" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
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
                var projects = await _projectLogic.ReadList(new ProjectSearchModel
                {
                    UserId = userId
                });

                return Ok(projects);
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
                var project = await _projectLogic.ReadElement(new ProjectSearchModel
                {
                    Id = id
                });

                if (project == null)
                {
                    return NotFound(new { message = "Проект не найден" });
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectModel request)
        {
            try
            {
                var existingProject = await _projectLogic.ReadElement(new ProjectSearchModel
                {
                    Id = id
                });

                if (existingProject == null)
                {
                    return NotFound(new { message = "Проект не найден" });
                }

                existingProject.Title = request.Title ?? existingProject.Title;
                existingProject.UpdatedAt = DateTime.UtcNow;

                var result = await _projectLogic.Update(existingProject);

                if (result)
                {
                    return Ok(new { message = "Проект обновлён" });
                }

                return BadRequest(new { message = "Ошибка при обновлении проекта" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
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
                var result = await _projectLogic.Delete(id);

                if (!result)
                {
                    return NotFound(new { message = "Проект не найден" });
                }

                return Ok(new { message = "Проект удалён" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
