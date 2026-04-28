using Database.Models;
using DataModels.AssemblyModels;
using DataModels.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class AssemblyStorage: IAssemblyStorage
    {
        private readonly ILogger<AssemblyStorage> _logger;

        public AssemblyStorage(ILogger<AssemblyStorage> logger)
        {
            _logger = logger;
        }

        public async Task<List<AssemblyModel>> GetFullList()
        {
            _logger.LogInformation($"Get full assembly list request received");

            try
            {
                using var context = new Database();
                var result = await context.Assemblies.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get full assembly list success");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get full assembly list failed, unexpected error");
                throw;
            }
        }

        public async Task<List<AssemblyModel>> GetFilteredList(AssemblySearchModel model)
        {
            _logger.LogInformation($"Get filtered assembly list request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                using var context = new Database();

                var query = context.Assemblies.AsQueryable();

                if (model.Id.HasValue)
                {
                    query = query.Where(x => x.Id == model.Id.Value);
                }
                if (model.ProjectId.HasValue)
                {
                    query = query.Where(x => x.ProjectId == model.ProjectId.Value);
                }
                //TODO: make filter by created date and update date (maybe)

                var result = await query.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get filtered assembly list success, assemblyID={model.Id}, projectID={model.ProjectId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get filtered assembly list failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<AssemblyModel?> GetElement(AssemblySearchModel model)
        {
            _logger.LogInformation($"Get assembly element request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                using var context = new Database();

                if (model.Id.HasValue)
                {
                    var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                    if (assembly == null)
                    {
                        _logger.LogWarning($"Get assembly element failed, assembly not found, assemblyID={model.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get assembly element success, assemblyID={model.Id}");
                    }

                    return assembly?.GetViewModel;
                }

                if (model.ProjectId.HasValue)
                {
                    var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.ProjectId == model.ProjectId.Value);

                    if (assembly == null)
                    {
                        _logger.LogWarning($"Get assembly element failed, assembly not found, projectID={model.ProjectId}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get assembly element success, projectID={model.ProjectId}");
                    }

                    return assembly?.GetViewModel;
                }
                //TODO: make filter by created date and notes (maybe again)

                _logger.LogWarning($"Get assembly element failed, search parameters are empty");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get assembly element failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<AssemblyModel?> Insert(AssemblyModel model)
        {
            _logger.LogInformation($"Insert assembly request received, projectID={model?.ProjectId}");

            try
            {
                var assembly = Assembly.Create(model);
                if (assembly == null)
                {
                    _logger.LogWarning($"Insert assembly failed, assembly create returned null, projectID={model?.ProjectId}");
                    return null;
                }
                using var context = new Database();
                await context.Assemblies.AddAsync(assembly);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Insert assembly success, assemblyID={assembly.Id}, projectID={model.ProjectId}");
                return assembly.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Insert assembly failed, unexpected error, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<AssemblyModel?> Update(AssemblyModel model)
        {
            _logger.LogInformation($"Update assembly request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                using var context = new Database();
                var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (assembly == null)
                {
                    _logger.LogWarning($"Update assembly failed, assembly not found, assemblyID={model.Id}");
                    return null;
                }
                assembly.Update(model);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Update assembly success, assemblyID={model.Id}");
                return assembly.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update assembly failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<AssemblyModel?> Delete(int id)
        {
            _logger.LogInformation($"Delete assembly request received, assemblyID={id}");

            try
            {
                using var context = new Database();
                var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == id);
                if (assembly == null)
                {
                    _logger.LogWarning($"Delete assembly failed, assembly not found, assemblyID={id}");
                    return null;
                }
                context.Assemblies.Remove(assembly);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Delete assembly success, assemblyID={id}");
                return assembly.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete assembly failed, unexpected error, assemblyID={id}");
                throw;
            }
        }
    }
}