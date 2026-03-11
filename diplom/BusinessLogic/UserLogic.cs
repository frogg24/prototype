using Database.Implements;
using DataModels;
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
        public UserLogic(UserStorage userStorage)
        {
            _userStorage = userStorage;
        }

        public async Task<List<UserModel>?> ReadList(UserSearchModel? model)
        {
            var list = model == null
                ? await _userStorage.GetFullList()
                : await _userStorage.GetFilteredList(model);

            if (list == null)
            {
                return null;
            }

            return list;
        }

        public async Task<UserModel?> ReadElement(UserSearchModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var element = await _userStorage.GetElement(model);

            if (element == null)
            {
                return null;
            }

            return element;
        }

        public async Task<bool> Create(UserModel model)
        {
            await CheckModelAsync(model);
            var result = await _userStorage.Insert(model);
            return result != null;
        }

        public async Task<bool> Update(UserModel model)
        {
            await CheckModelAsync(model);
            var result = await _userStorage.Update(model);
            return result != null;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _userStorage.Delete(id);

            if (result == null)
            {
                return false;
            }

            return true;
        }

        private async Task CheckModelAsync(UserModel model, bool withParams = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                throw new ArgumentException("Логин не может быть пустым", nameof(model.Username));
            }
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                throw new ArgumentException("Почта не может быть пустой", nameof(model.Email));
            }
            if (!new EmailAddressAttribute().IsValid(model.Email))
            {
                throw new ArgumentException("Некорректный формат почты", nameof(model.Email));
            }
            if (string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                throw new ArgumentException("Пароль не может быть пустым", nameof(model.PasswordHash));
            }

            var existingUser = await _userStorage.GetElement(new UserSearchModel { Email = model.Email });

            if (existingUser != null && existingUser.Id != model.Id &&
                string.Equals(existingUser.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Пользователь с почтой '{model.Email}' уже существует");
            }
        }
    }
}
