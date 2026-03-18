using BusinessLogic;
using DataModels.UserModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserLogic _userlogic;

        public UserController(UserLogic userlogic)
        {
            _userlogic = userlogic;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestModel request)
        {
            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "Пароли не совпадают" });
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var userModel = new UserModel
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                };

                var result = await _userlogic.Create(userModel);

                if (result)
                {
                    return Ok(new { message = "Регистрация успешна" });
                }

                return BadRequest(new { message = "Ошибка при регистрации" });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message + ex.InnerException?.Message + ex.InnerException?.StackTrace });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            try
            {
                var searchModel = new UserSearchModel { Email = request.Email };
                var user = await _userlogic.ReadElement(searchModel);

                if (user == null)
                {
                    return Unauthorized(new { message = "Пользователь не найден" });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Неверный пароль" });
                }

                return Ok(new
                {
                    message = "Вход выполнен успешно",
                    user = new { user.Id, user.Username, user.Email, user.CreatedAt }
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userlogic.ReadList(null);

                // Не возвращаем пароли
                var safeUsers = users?.Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email
                }).ToList();

                return Ok(safeUsers);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var searchModel = new UserSearchModel { Id = id };
                var user = await _userlogic.ReadElement(searchModel);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Не возвращаем пароль
                var safeUser = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.CreatedAt
                };

                return Ok(safeUser);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        //TODO: make model to update and make password update
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserSearchModel request)
        {
            try
            {
                // Получаем текущего пользователя
                var searchModel = new UserSearchModel { Id = id };
                var existingUser = await _userlogic.ReadElement(searchModel);

                if (existingUser == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Обновляем данные
                existingUser.Username = request.Username ?? existingUser.Username;
                existingUser.Email = request.Email ?? existingUser.Email;

                var result = await _userlogic.Update(existingUser);

                if (result)
                {
                    return Ok(new { message = "Данные пользователя обновлены" });
                }

                return BadRequest(new { message = "Ошибка при обновлении" });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var searchModel = new UserSearchModel { Id = id };
                var user = await _userlogic.ReadElement(searchModel);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var result = await _userlogic.Delete(user.Id);

                if (result)
                {
                    return Ok(new { message = "Пользователь удален" });
                }

                return BadRequest(new { message = "Ошибка при удалении" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? email = null, [FromQuery] string? username = null)
        {
            try
            {
                var searchModel = new UserSearchModel
                {
                    Email = email,
                    Username = username
                };

                var users = await _userlogic.ReadList(searchModel);

                // Не возвращаем пароли
                var safeUsers = users?.Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email
                }).ToList();

                return Ok(safeUsers);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
