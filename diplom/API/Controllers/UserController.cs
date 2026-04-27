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
        private readonly ILogger<UserController> _logger;

        public UserController(UserLogic userlogic, ILogger<UserController> logger)
        {
            _userlogic = userlogic;
            _logger=logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestModel request)
        {
            _logger.LogInformation($"Register request received. Email={request.Email}, Username={request.Username}");

            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    _logger.LogWarning($"Register failed: passwords mismatch. Email={request.Email}");

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
                    _logger.LogInformation($"User registered successfully. Email={request.Email}");

                    return Ok(new { message = "Регистрация успешна" });
                }

                _logger.LogInformation($"User registration failed. Email={request.Email}");
                return BadRequest(new { message = "Ошибка при регистрации" });
            }
            catch (System.ArgumentException ex)
            {
                _logger.LogWarning($"Validation error during register. Email={request.Email}");
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                _logger.LogWarning($"Conflict during registration. Email={request.Email}");
                return Conflict(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"unexpected error during registration. Email={request.Email}");
                return StatusCode(500, new { message = ex.Message + ex.InnerException?.Message + ex.InnerException?.StackTrace });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            _logger.LogInformation($"Login request received. Email={request.Email}");
            try
            {
                var searchModel = new UserSearchModel { Email = request.Email };
                var user = await _userlogic.ReadElement(searchModel);

                if (user == null)
                {
                    _logger.LogWarning($"Login failed. User not found. Email={request.Email}");
                    return Unauthorized(new { message = "Пользователь не найден" });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login failed. Incorrect password. Email={request.Email}");
                    return Unauthorized(new { message = "Неверный пароль" });
                }

                _logger.LogInformation($"Login sucess. Email={request.Email}");
                return Ok(new
                {
                    message = "Вход выполнен успешно",
                    user = new { user.Id, user.Username, user.Email, user.CreatedAt }
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Unexpacted error during login. Email={request.Email}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation($"Get all users request received");
            try
            {
                var users = await _userlogic.ReadList(null);

                // Не возвращаем пароли
                var safeUsers = users?.Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email
                }).ToList();

                _logger.LogInformation($"Get all users success");
                return Ok(safeUsers);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Unexpected error during");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            _logger.LogInformation($"Get user by ID request received");
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

                _logger.LogInformation($"Get user by ID success");
                return Ok(safeUser);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Unexpected error during getting user by ID");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        //TODO: make model to update and make password update
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserSearchModel request)
        {
            _logger.LogInformation($"Update user request received, user ID={id}");
            try
            {
                // Получаем текущего пользователя
                var searchModel = new UserSearchModel { Id = id };
                var existingUser = await _userlogic.ReadElement(searchModel);

                if (existingUser == null)
                {
                    _logger.LogWarning($"Update failed, user not found, user ID={id}");
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Обновляем данные
                existingUser.Username = request.Username ?? existingUser.Username;
                existingUser.Email = request.Email ?? existingUser.Email;

                var result = await _userlogic.Update(existingUser);

                if (result)
                {
                    _logger.LogInformation($"Update success, user ID={id}");
                    return Ok(new { message = "Данные пользователя обновлены" });
                }

                _logger.LogWarning($"Update failed, unexpected error, user ID={id}");
                return BadRequest(new { message = "Ошибка при обновлении" });
            }
            catch (System.ArgumentException ex)
            {
                _logger.LogWarning($"Update failed, validation failed, user ID={id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                _logger.LogWarning($"Update failed, conflict, user ID={id}");
                return Conflict(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Update failed, unexpected error, user ID={id}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation($"Delete user request received, user ID={id}");
            try
            {
                var searchModel = new UserSearchModel { Id = id };
                var user = await _userlogic.ReadElement(searchModel);

                if (user == null)
                {
                    _logger.LogWarning($"Delete failed, user not found, user ID={id}");
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var result = await _userlogic.Delete(user.Id);

                if (result)
                {
                    _logger.LogInformation($"Delete user success, user ID={id}");
                    return Ok(new { message = "Пользователь удален" });
                }

                _logger.LogWarning($"Delete failed, unexpected error, user ID={id}");
                return BadRequest(new { message = "Ошибка при удалении" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Delete failed, unexpected error, user ID={id}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? email = null, [FromQuery] string? username = null)
        {
            _logger.LogInformation($"Search user, unexpected error, email={email}, username={username}");
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

                _logger.LogInformation($"Search user success email={email}, username={username}");
                return Ok(safeUsers);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation($"Search user, unexpected error, email={email}, username={username}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
