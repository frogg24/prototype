using Database.Models;
using DataModels.ProjectModels;
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
    public class ProjectStorage
    {
        private readonly ILogger<ProjectStorage> _logger;

        public ProjectStorage(ILogger<ProjectStorage> logger)
        {
            _logger = logger;
        }

        public async Task<List<ProjectModel>> GetFullList()
        {
            _logger.LogInformation($"Get full project list request received");

            try
            {
                using var context = new Database();
                var result = await context.Projects.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get full project list success");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get full project list failed, unexpected error");
                throw;
            }
        }

        public async Task<List<ProjectModel>> GetFilteredList(ProjectSearchModel model)
        {
            _logger.LogInformation($"Get filtered project list request received, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");

            try
            {
                using var context = new Database();

                var query = context.Projects.AsQueryable();

                if (model.Id.HasValue)
                {
                    query = query.Where(x => x.Id == model.Id.Value);
                }
                if (model.UserId.HasValue)
                {
                    query = query.Where(x => x.UserId == model.UserId.Value);
                }
                if (!string.IsNullOrEmpty(model.Title))
                {
                    query = query.Where(x => x.Title.Equals(model.Title));
                }

                //TODO: make filter by created and updated dates

                var result = await query.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get filtered project list success, projectID={model.Id}, userID={model.UserId}, title={model.Title}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get filtered project list failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
        }

        public async Task<ProjectModel?> GetElement(ProjectSearchModel model)
        {
            _logger.LogInformation($"Get project element request received, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");

            try
            {
                using var context = new Database();

                if (model.Id.HasValue)
                {
                    var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                    if (project == null)
                    {
                        _logger.LogWarning($"Get project element failed, project not found, projectID={model.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get project element success, projectID={model.Id}");
                    }

                    return project?.GetViewModel;
                }

                if (model.Id.HasValue)
                {
                    var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                    if (project == null)
                    {
                        _logger.LogWarning($"Get project element failed, project not found, projectID={model.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get project element success, projectID={model.Id}");
                    }

                    return project?.GetViewModel;
                }

                if (!string.IsNullOrEmpty(model.Title))
                {
                    var project = await context.Projects.FirstOrDefaultAsync(x => x.Title.Equals(model.Title));

                    if (project == null)
                    {
                        _logger.LogWarning($"Get project element failed, project not found, title={model.Title}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get project element success, title={model.Title}");
                    }

                    return project?.GetViewModel;
                }

                //TODO: make filter by created and updated dates
                _logger.LogWarning($"Get project element failed, search parameters are empty");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get project element failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
        }

        public async Task<ProjectModel?> Insert(ProjectModel model)
        {
            _logger.LogInformation($"Insert project request received, userID={model?.UserId}, title={model?.Title}");

            try
            {
                var project = Project.Create(model);
                if (project == null)
                {
                    _logger.LogWarning($"Insert project failed, project create returned null, userID={model?.UserId}, title={model?.Title}");
                    return null;
                }
                using var context = new Database();
                await context.Projects.AddAsync(project);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Insert project success, projectID={project.Id}, userID={model.UserId}, title={model.Title}");
                return project.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Insert project failed, unexpected error, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
        }

        public async Task<ProjectModel?> Update(ProjectModel model)
        {
            _logger.LogInformation($"Update project request received, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");

            try
            {
                using var context = new Database();
                var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (project == null)
                {
                    _logger.LogWarning($"Update project failed, project not found, projectID={model.Id}");
                    return null;
                }
                project.Update(model);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Update project success, projectID={model.Id}");
                return project.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update project failed, unexpected error, projectID={model?.Id}, userID={model?.UserId}, title={model?.Title}");
                throw;
            }
        }

        public async Task<ProjectModel?> Delete(int id)
        {
            _logger.LogInformation($"Delete project request received, projectID={id}");

            try
            {
                using var context = new Database();
                var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == id);
                if (project == null)
                {
                    _logger.LogWarning($"Delete project failed, project not found, projectID={id}");
                    return null;
                }
                context.Projects.Remove(project);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Delete project success, projectID={id}");
                return project.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete project failed, unexpected error, projectID={id}");
                throw;
            }
        }
    }
}