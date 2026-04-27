using Database.Models;
using DataModels.UserModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class UserStorage
    {
        private readonly ILogger<UserStorage> _logger;

        public UserStorage(ILogger<UserStorage> logger)
        {
            _logger = logger;
        }

        public async Task<List<UserModel>> GetFullList()
        {
            _logger.LogInformation($"Get full user list request received");

            try
            {
                using var context = new Database();
                var result = await context.Users.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get full user list success");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get full user list failed, unexpected error");
                throw;
            }
        }

        public async Task<List<UserModel>> GetFilteredList(UserSearchModel model)
        {
            _logger.LogInformation($"Get filtered user list request received, userID={model?.Id}, username={model?.Username}, email={model?.Email}");

            try
            {
                using var context = new Database();

                var query = context.Users.AsQueryable();

                if (model.Id.HasValue)
                {
                    query = query.Where(x => x.Id == model.Id.Value);
                }
                if (!string.IsNullOrEmpty(model.Username))
                {
                    query = query.Where(x => x.Username.Equals(model.Username));
                }
                if (!string.IsNullOrEmpty(model.Email))
                {
                    query = query.Where(x => x.Email.Equals(model.Email));
                }
                //TODO: make filter by created date

                var result = await query.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get filtered user list success, userID={model.Id}, username={model.Username}, email={model.Email}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get filtered user list failed, unexpected error, userID={model?.Id}, username={model?.Username}, email={model?.Email}");
                throw;
            }
        }

        public async Task<UserModel?> GetElement(UserSearchModel model)
        {
            _logger.LogInformation($"Get user element request received, userID={model?.Id}, username={model?.Username}, email={model?.Email}");

            try
            {
                using var context = new Database();

                if (model.Id.HasValue)
                {
                    var user = await context.Users.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                    if (user == null)
                    {
                        _logger.LogWarning($"Get user element failed, user not found, userID={model.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get user element success, userID={model.Id}");
                    }

                    return user?.GetViewModel;
                }

                if (!string.IsNullOrEmpty(model.Username))
                {
                    var user = await context.Users.FirstOrDefaultAsync(x => x.Username.Equals(model.Username));

                    if (user == null)
                    {
                        _logger.LogWarning($"Get user element failed, user not found, username={model.Username}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get user element success, username={model.Username}");
                    }

                    return user?.GetViewModel;
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(model.Email));

                    if (user == null)
                    {
                        _logger.LogWarning($"Get user element failed, user not found, email={model.Email}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get user element success, email={model.Email}");
                    }

                    return user?.GetViewModel;
                }
                //TODO: make filter by created date

                _logger.LogWarning($"Get user element failed, search parameters are empty");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get user element failed, unexpected error, userID={model?.Id}, username={model?.Username}, email={model?.Email}");
                throw;
            }
        }

        public async Task<UserModel?> Insert(UserModel model)
        {
            _logger.LogInformation($"Insert user request received, username={model?.Username}, email={model?.Email}");

            try
            {
                var user = User.Create(model);
                if (user == null)
                {
                    _logger.LogWarning($"Insert user failed, user create returned null, username={model?.Username}, email={model?.Email}");
                    return null;
                }
                using var context = new Database();
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Insert user success, userID={user.Id}, username={model.Username}, email={model.Email}");
                return user.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Insert user failed, unexpected error, username={model?.Username}, email={model?.Email}");
                throw;
            }
        }

        public async Task<UserModel?> Update(UserModel model)
        {
            _logger.LogInformation($"Update user request received, userID={model?.Id}, username={model?.Username}, email={model?.Email}");

            try
            {
                using var context = new Database();
                var User = await context.Users.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (User == null)
                {
                    _logger.LogWarning($"Update user failed, user not found, userID={model.Id}");
                    return null;
                }
                User.Update(model);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Update user success, userID={model.Id}");
                return User.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update user failed, unexpected error, userID={model?.Id}, username={model?.Username}, email={model?.Email}");
                throw;
            }
        }

        public async Task<UserModel?> Delete(int id)
        {
            _logger.LogInformation($"Delete user request received, userID={id}");

            try
            {
                using var context = new Database();
                var User = await context.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (User == null)
                {
                    _logger.LogWarning($"Delete user failed, user not found, userID={id}");
                    return null;
                }
                context.Users.Remove(User);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Delete user success, userID={id}");
                return User.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete user failed, unexpected error, userID={id}");
                throw;
            }
        }
    }
}