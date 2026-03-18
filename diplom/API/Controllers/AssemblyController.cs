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

        public AssemblyController(AssemblyLogic assemblyLogic)
        {
            _assemblyLogic = assemblyLogic;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AssemblyModel model)
        {
            try
            {
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                var result = await _assemblyLogic.Create(model);

                if (result)
                {
                    return Ok(new { message = "Сборка успешно создана" });
                }

                return BadRequest(new { message = "Ошибка при создании сборки" });
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

        [HttpGet("project/{projectId:long}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            try
            {
                var assemblies = await _assemblyLogic.ReadList(new AssemblySearchModel
                {
                    ProjectId = projectId
                });

                return Ok(assemblies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var assembly = await _assemblyLogic.ReadElement(new AssemblySearchModel
                {
                    Id = id
                });

                if (assembly == null)
                {
                    return NotFound(new { message = "Сборка не найдена" });
                }

                return Ok(assembly);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AssemblyModel request)
        {
            try
            {
                var existingAssembly = await _assemblyLogic.ReadElement(new AssemblySearchModel
                {
                    Id = id
                });

                if (existingAssembly == null)
                {
                    return NotFound(new { message = "Сборка не найдена" });
                }

                existingAssembly.ConsensusSequence = request.ConsensusSequence;
                existingAssembly.ConsensusLength = request.ConsensusLength;
                existingAssembly.UpdatedAt = DateTime.UtcNow;

                var result = await _assemblyLogic.Update(existingAssembly);

                if (result)
                {
                    return Ok(new { message = "Сборка обновлена" });
                }

                return BadRequest(new { message = "Ошибка при обновлении сборки" });
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
                var result = await _assemblyLogic.Delete(id);

                if (!result)
                {
                    return NotFound(new { message = "Сборка не найдена" });
                }

                return Ok(new { message = "Сборка удалена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("project/{projectId:int}/run")]
        public async Task<IActionResult> RunAssembly(int projectId)
        {
            try
            {
                var assembly = await _assemblyLogic.MakeOLC(projectId);

                if (assembly == null)
                {
                    return BadRequest(new { message = "Не удалось выполнить сборку" });
                }

                return Ok(assembly);
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
    }
}