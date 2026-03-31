using Database.Implements;
using DataModels.AssemblyModels;
using DataModels.enums;
using DataModels.ReadModels;
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
        private readonly AssemblyStorage _assemblyStorage;
        private readonly AlgorithmOLC _algorithmOLC;

        public AssemblyLogic(AssemblyStorage assemblyStorage, AlgorithmOLC algorithmOLC)
        {
            _assemblyStorage = assemblyStorage;
            _algorithmOLC=algorithmOLC;
        }

        public async Task<AssemblyModel> MakeOLC(int projectId)
        {
            if (projectId <= 0)
            {
                throw new ArgumentException("Некорректный ID проекта");
            }
            AssemblyModel assembly = await _algorithmOLC.OLC(projectId);

            if (string.IsNullOrWhiteSpace(assembly.ConsensusSequence))
            {
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
                    throw new InvalidOperationException("Не удалось сохранить сборку");

                return created;
            }

            existing.ConsensusSequence = assembly.ConsensusSequence;
            existing.ConsensusLength = assembly.ConsensusLength;
            existing.QualityValuesJson = assembly.QualityValuesJson;
            existing.TraceDataJson = assembly.TraceDataJson;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _assemblyStorage.Update(existing);
            if (updated == null)
                throw new InvalidOperationException("Не удалось обновить сборку");

            return updated;
        }
        public async Task<List<AssemblyModel>> GetProjectAssemblysAsync(int projectId)
        {
            return await _assemblyStorage.GetFilteredList(new AssemblySearchModel { ProjectId = projectId });
        }

        public async Task<List<AssemblyModel>?> ReadList(AssemblySearchModel? model)
        {
            var list = model == null
                ? await _assemblyStorage.GetFullList()
                : await _assemblyStorage.GetFilteredList(model);

            if (list == null)
            {
                return null;
            }

            return list;
        }

        public async Task<AssemblyModel?> ReadElement(AssemblySearchModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var element = await _assemblyStorage.GetElement(model);

            if (element == null)
            {
                return null;
            }

            return element;
        }

        public async Task<bool> Create(AssemblyModel model)
        {
            await CheckModelAsync(model);
            var result = await _assemblyStorage.Insert(model);
            return result != null;
        }

        public async Task<bool> Update(AssemblyModel model)
        {
            await CheckModelAsync(model);
            var result = await _assemblyStorage.Update(model);
            return result != null;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _assemblyStorage.Delete(id);

            if (result == null)
            {
                return false;
            }

            return true;
        }

        private async Task CheckModelAsync(AssemblyModel model, bool withParams = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                return;
            }

            if (model.ProjectId <=0 )
            {
                throw new ArgumentException("некорректный ID проекта", nameof(model.ProjectId));
            }
        }
    }
}
