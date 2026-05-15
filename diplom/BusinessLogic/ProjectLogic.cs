using Database.Implements;
using DataModels.Interfaces;
using DataModels.ProjectModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ProjectLogic
    {
        private readonly IProjectStorage _projectStorage;
        private readonly ILogger<ProjectLogic> _logger;

        public ProjectLogic(IProjectStorage projectStorage, ILogger<ProjectLogic> logger)
        {
            _projectStorage = projectStorage;
            _logger = logger;
        }

        public async Task<List<ProjectModel>?> ReadList(ProjectSearchModel? model)
        {
            _logger.LogInformation($"Read project list request received, projectID={model?.Id}, userID={model?.UserId}");

            try
            {
                var list = model == null
                    ? await _projectStorage.GetFullList()
                    : await _projectStorage.GetFilteredList(model);

                if (list == null)
                {
                    _logger.LogWarning($"Read project list failed, list is null, projectID={model?.Id}, userID={model?.UserId}");
                    return null;
                }

                _logger.LogInformation($"Read project list success, projectID={model?.Id}, userID={model?.UserId}");
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read project list failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}");
                throw;
            }
        }

        public async Task<ProjectModel?> ReadElement(ProjectSearchModel model)
        {
            _logger.LogInformation($"Read project element request received, projectID={model?.Id}, userID={model?.UserId}");

            try
            {
                if (model == null)
                {
                    _logger.LogWarning($"Read project element failed, search model is null");
                    throw new ArgumentNullException(nameof(model));
                }

                var element = await _projectStorage.GetElement(model);

                if (element == null)
                {
                    _logger.LogWarning($"Read project element failed, project not found, projectID={model.Id}, userID={model.UserId}");
                    return null;
                }

                _logger.LogInformation($"Read project element success, projectID={element.Id}, userID={element.UserId}");
                return element;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning($"Read project element failed, search model is null");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read project element failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}");
                throw;
            }
        }

        public async Task<bool> Create(ProjectModel model)
        {
            _logger.LogInformation($"Create project request received, userID={model?.UserId}, title={model?.Title}");

            try
            {
                await CheckModelAsync(model);
                var result = await _projectStorage.Insert(model);

                if (result == null)
                {
                    _logger.LogWarning($"Create project failed, userID={model.UserId}, title={model.Title}");
                }
                else
                {
                    _logger.LogInformation($"Create project success, projectID={result.Id}, userID={result.UserId}");
                }

                return result != null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create project failed, validation error, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create project failed, conflict, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create project failed, unexpected error, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
        }

        public async Task<bool> Update(ProjectModel model)
        {
            _logger.LogInformation($"Update project request received, projectID={model?.Id}, userID={model?.UserId}");

            try
            {
                await CheckModelAsync(model);
                var result = await _projectStorage.Update(model);

                if (result == null)
                {
                    _logger.LogWarning($"Update project failed, projectID={model.Id}, userID={model.UserId}");
                }
                else
                {
                    _logger.LogInformation($"Update project success, projectID={result.Id}, userID={result.UserId}");
                }

                return result != null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update project failed, validation error, projectID={model?.Id}, userID={model?.UserId}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update project failed, conflict, projectID={model?.Id}, userID={model?.UserId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update project failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}");
                throw;
            }
        }

        public async Task<bool> Delete(int id)
        {
            _logger.LogInformation($"Delete project request received, projectID={id}");

            try
            {
                var result = await _projectStorage.Delete(id);

                if (result == null)
                {
                    _logger.LogWarning($"Delete project failed, project not found, projectID={id}");
                    return false;
                }

                _logger.LogInformation($"Delete project success, projectID={id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete project failed, unexpected error, projectID={id}");
                throw;
            }
        }

        private async Task CheckModelAsync(ProjectModel model, bool withParams = true)
        {
            _logger.LogInformation($"Check project model request received, projectID={model?.Id}, userID={model?.UserId}");

            if (model == null)
            {
                _logger.LogWarning($"Check project model failed, model is null");
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                _logger.LogInformation($"Check project model success without params, projectID={model.Id}, userID={model.UserId}");
                return;
            }

            if (model.UserId <= 0)
            {
                _logger.LogWarning($"Check project model failed, incorrect user ID, userID={model.UserId}");
                throw new ArgumentException("некорректный ID пользователя", nameof(model.UserId));
            }

            _logger.LogInformation($"Check project model success, projectID={model.Id}, userID={model.UserId}");
        }
    }
}