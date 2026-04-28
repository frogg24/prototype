using Database.Implements;
using DataModels.UserModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class UserLogic
    {
        private readonly UserStorage _userStorage;
        private readonly ILogger<UserLogic> _logger;

        public UserLogic(UserStorage userStorage, ILogger<UserLogic> logger)
        {
            _userStorage = userStorage;
            _logger = logger;
        }

        public async Task<List<UserModel>?> ReadList(UserSearchModel? model)
        {
            _logger.LogInformation($"Read user list request received");

            try
            {
                var list = model == null
                    ? await _userStorage.GetFullList()
                    : await _userStorage.GetFilteredList(model);

                if (list == null)
                {
                    _logger.LogWarning($"Read user list failed, list is null");
                    return null;
                }

                _logger.LogInformation($"Read user list success");
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read user list failed, unexpected error");
                throw;
            }
        }

        public async Task<UserModel?> ReadElement(UserSearchModel model)
        {
            _logger.LogInformation($"Read user element request received, userID={model?.Id}, email={model?.Email}");

            if (model == null)
            {
                _logger.LogWarning($"Read user element failed, search model is null");
                throw new ArgumentNullException(nameof(model));
            }

            try
            {
                var element = await _userStorage.GetElement(model);

                if (element == null)
                {
                    _logger.LogWarning($"Read user element failed, user not found, userID={model.Id}, email={model.Email}");
                    return null;
                }

                _logger.LogInformation($"Read user element success, userID={element.Id}, email={element.Email}");
                return element;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read user element failed, unexpected error, userID={model.Id}, email={model.Email}");
                throw;
            }
        }

        public async Task<bool> Create(UserModel model)
        {
            _logger.LogInformation($"Create user request received, email={model?.Email}, username={model?.Username}");

            await CheckModelAsync(model);

            try
            {
                var result = await _userStorage.Insert(model);

                if (result == null)
                {
                    _logger.LogWarning($"Create user failed, email={model.Email}, username={model.Username}");
                    return false;
                }

                _logger.LogInformation($"Create user success, email={model.Email}, username={model.Username}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create user failed, unexpected error, email={model.Email}, username={model.Username}");
                throw;
            }
        }

        public async Task<bool> Update(UserModel model)
        {
            _logger.LogInformation($"Update user request received, userID={model?.Id}, email={model?.Email}");

            await CheckModelAsync(model);

            try
            {
                var result = await _userStorage.Update(model);

                if (result == null)
                {
                    _logger.LogWarning($"Update user failed, userID={model.Id}, email={model.Email}");
                    return false;
                }

                _logger.LogInformation($"Update user success, userID={model.Id}, email={model.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update user failed, unexpected error, userID={model.Id}, email={model.Email}");
                throw;
            }
        }

        public async Task<bool> Delete(int id)
        {
            _logger.LogInformation($"Delete user request received, userID={id}");

            try
            {
                var result = await _userStorage.Delete(id);

                if (result == null)
                {
                    _logger.LogWarning($"Delete user failed, user not found, userID={id}");
                    return false;
                }

                _logger.LogInformation($"Delete user success, userID={id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete user failed, unexpected error, userID={id}");
                throw;
            }
        }

        private async Task CheckModelAsync(UserModel model, bool withParams = true)
        {
            _logger.LogInformation($"Check user model request received, userID={model?.Id}, email={model?.Email}");

            if (model == null)
            {
                _logger.LogWarning($"Check user model failed, model is null");
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                _logger.LogInformation($"Check user model success without params, userID={model.Id}, email={model.Email}");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                _logger.LogWarning($"Check user model failed, username is empty, email={model.Email}");
                throw new ArgumentException("Логин не может быть пустым", nameof(model.Username));
            }
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                _logger.LogWarning($"Check user model failed, email is empty, userID={model.Id}, username={model.Username}");
                throw new ArgumentException("Почта не может быть пустой", nameof(model.Email));
            }
            if (!new EmailAddressAttribute().IsValid(model.Email))
            {
                _logger.LogWarning($"Check user model failed, incorrect email format, email={model.Email}");
                throw new ArgumentException("Некорректный формат почты", nameof(model.Email));
            }
            if (string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                _logger.LogWarning($"Check user model failed, password hash is empty, email={model.Email}");
                throw new ArgumentException("Пароль не может быть пустым", nameof(model.PasswordHash));
            }

            var existingUser = await _userStorage.GetElement(new UserSearchModel { Email = model.Email });

            if (existingUser != null && existingUser.Id != model.Id &&
                string.Equals(existingUser.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Check user model failed, user with email already exists, email={model.Email}");
                throw new InvalidOperationException($"Пользователь с почтой '{model.Email}' уже существует");
            }

            _logger.LogInformation($"Check user model success, userID={model.Id}, email={model.Email}");
        }
    }
}