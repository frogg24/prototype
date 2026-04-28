using Database.Implements;
using DataModels.AssemblyModels;
using DataModels.enums;
using DataModels.Interfaces;
using DataModels.ReadModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class AssemblyLogic
    {
        private readonly IAssemblyStorage _assemblyStorage;
        private readonly AlgorithmOLC _algorithmOLC;
        private readonly ILogger<AssemblyLogic> _logger;

        public AssemblyLogic(IAssemblyStorage assemblyStorage, AlgorithmOLC algorithmOLC, ILogger<AssemblyLogic> logger)
        {
            _assemblyStorage = assemblyStorage;
            _algorithmOLC=algorithmOLC;
            _logger = logger;
        }

        public async Task<AssemblyModel> MakeOLC(int projectId)
        {
            _logger.LogInformation($"Make OLC request received, projectID={projectId}");

            try
            {
                if (projectId <= 0)
                {
                    _logger.LogWarning($"Make OLC failed, incorrect project ID, projectID={projectId}");
                    throw new ArgumentException("Некорректный ID проекта");
                }
                AssemblyModel assembly = await _algorithmOLC.OLC(projectId);

                if (string.IsNullOrWhiteSpace(assembly.ConsensusSequence))
                {
                    _logger.LogWarning($"Make OLC failed, consensus sequence is empty, projectID={projectId}");
                    throw new InvalidOperationException("Алгоритм не вернул консенсусную последовательность");
                }

                assembly.ProjectId = projectId;
                assembly.ConsensusLength = assembly.ConsensusSequence.Length;
                assembly.UpdatedAt = DateTime.UtcNow;

                var existing = await _assemblyStorage.GetElement(new AssemblySearchModel
                {
                    ProjectId = projectId
                });

                if (existing == null)
                {
                    assembly.CreatedAt = DateTime.UtcNow;
                    var created = await _assemblyStorage.Insert(assembly);
                    if (created == null)
                    {
                        _logger.LogWarning($"Make OLC failed, assembly was not saved, projectID={projectId}");
                        throw new InvalidOperationException("Не удалось сохранить сборку");
                    }

                    _logger.LogInformation($"Make OLC success, assembly created, projectID={projectId}, assemblyID={created.Id}");
                    return created;
                }

                existing.ConsensusSequence = assembly.ConsensusSequence;
                existing.ConsensusLength = assembly.ConsensusLength;
                existing.QualityValuesJson = assembly.QualityValuesJson;
                existing.TraceDataJson = assembly.TraceDataJson;
                existing.UpdatedAt = DateTime.UtcNow;

                var updated = await _assemblyStorage.Update(existing);
                if (updated == null)
                {
                    _logger.LogWarning($"Make OLC failed, assembly was not updated, projectID={projectId}, assemblyID={existing.Id}");
                    throw new InvalidOperationException("Не удалось обновить сборку");
                }

                _logger.LogInformation($"Make OLC success, assembly updated, projectID={projectId}, assemblyID={updated.Id}");
                return updated;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Make OLC failed, validation error, projectID={projectId}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Make OLC failed, conflict, projectID={projectId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Make OLC failed, unexpected error, projectID={projectId}");
                throw;
            }
        }
        public async Task<List<AssemblyModel>> GetProjectAssemblysAsync(int projectId)
        {
            _logger.LogInformation($"Get project assemblies request received, projectID={projectId}");

            try
            {
                var result = await _assemblyStorage.GetFilteredList(new AssemblySearchModel { ProjectId = projectId });

                _logger.LogInformation($"Get project assemblies success, projectID={projectId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get project assemblies failed, unexpected error, projectID={projectId}");
                throw;
            }
        }

        public async Task<List<AssemblyModel>?> ReadList(AssemblySearchModel? model)
        {
            _logger.LogInformation($"Read assembly list request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                var list = model == null
                    ? await _assemblyStorage.GetFullList()
                    : await _assemblyStorage.GetFilteredList(model);

                if (list == null)
                {
                    _logger.LogWarning($"Read assembly list failed, list is null, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                    return null;
                }

                _logger.LogInformation($"Read assembly list success, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read assembly list failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<AssemblyModel?> ReadElement(AssemblySearchModel model)
        {
            _logger.LogInformation($"Read assembly element request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                if (model == null)
                {
                    _logger.LogWarning($"Read assembly element failed, search model is null");
                    throw new ArgumentNullException(nameof(model));
                }

                var element = await _assemblyStorage.GetElement(model);

                if (element == null)
                {
                    _logger.LogWarning($"Read assembly element failed, assembly not found, assemblyID={model.Id}, projectID={model.ProjectId}");
                    return null;
                }

                _logger.LogInformation($"Read assembly element success, assemblyID={element.Id}, projectID={element.ProjectId}");
                return element;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning($"Read assembly element failed, search model is null");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read assembly element failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<bool> Create(AssemblyModel model)
        {
            _logger.LogInformation($"Create assembly request received, projectID={model?.ProjectId}");

            try
            {
                await CheckModelAsync(model);
                var result = await _assemblyStorage.Insert(model);

                if (result == null)
                {
                    _logger.LogWarning($"Create assembly failed, projectID={model.ProjectId}");
                }
                else
                {
                    _logger.LogInformation($"Create assembly success, assemblyID={result.Id}, projectID={result.ProjectId}");
                }

                return result != null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create assembly failed, validation error, projectID={model?.ProjectId}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create assembly failed, conflict, projectID={model?.ProjectId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create assembly failed, unexpected error, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<bool> Update(AssemblyModel model)
        {
            _logger.LogInformation($"Update assembly request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                await CheckModelAsync(model);
                var result = await _assemblyStorage.Update(model);

                if (result == null)
                {
                    _logger.LogWarning($"Update assembly failed, assemblyID={model.Id}, projectID={model.ProjectId}");
                }
                else
                {
                    _logger.LogInformation($"Update assembly success, assemblyID={result.Id}, projectID={result.ProjectId}");
                }

                return result != null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update assembly failed, validation error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update assembly failed, conflict, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update assembly failed, unexpected error, assemblyID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<bool> Delete(int id)
        {
            _logger.LogInformation($"Delete assembly request received, assemblyID={id}");

            try
            {
                var result = await _assemblyStorage.Delete(id);

                if (result == null)
                {
                    _logger.LogWarning($"Delete assembly failed, assembly not found, assemblyID={id}");
                    return false;
                }

                _logger.LogInformation($"Delete assembly success, assemblyID={id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete assembly failed, unexpected error, assemblyID={id}");
                throw;
            }
        }

        private async Task CheckModelAsync(AssemblyModel model, bool withParams = true)
        {
            _logger.LogInformation($"Check assembly model request received, assemblyID={model?.Id}, projectID={model?.ProjectId}");

            if (model == null)
            {
                _logger.LogWarning($"Check assembly model failed, model is null");
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                _logger.LogInformation($"Check assembly model success without params, assemblyID={model.Id}, projectID={model.ProjectId}");
                return;
            }

            if (model.ProjectId <=0)
            {
                _logger.LogWarning($"Check assembly model failed, incorrect project ID, projectID={model.ProjectId}");
                throw new ArgumentException("некорректный ID проекта", nameof(model.ProjectId));
            }

            _logger.LogInformation($"Check assembly model success, assemblyID={model.Id}, projectID={model.ProjectId}");
        }
    }
}