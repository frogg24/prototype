using Database.Implements;
using DataModels.enums;
using DataModels.ReadModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ReadLogic
    {
        private readonly ReadStorage _readStorage;
        private readonly ILogger<ReadLogic> _logger;

        public ReadLogic(ReadStorage readStorage, ILogger<ReadLogic> logger)
        {
            _readStorage = readStorage;
            _logger = logger;
        }

        public async Task<List<ReadModel>> UploadReadsAsync(int? projectId, IEnumerable<UploadReadFileModel> files)
        {
            _logger.LogInformation($"Upload reads request received, projectID={projectId}");

            try
            {
                var uploaded = new List<ReadModel>();

                foreach (var file in files)
                {
                    if (file.Content.Length == 0)
                    {
                        _logger.LogWarning($"Upload read file skipped, empty content, projectID={projectId}, fileName={file.FileName}");
                        continue;
                    }

                    if (!string.Equals(Path.GetExtension(file.FileName), ".ab1", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Upload read file failed, wrong file format, projectID={projectId}, fileName={file.FileName}");
                        throw new InvalidDataException($"Файл {file.FileName} должен быть в формате .ab1");
                    }

                    var parsed = Ab1Parser.Parse(file.Content);

                    var readModel = new ReadModel
                    {
                        FileName = Path.GetFileName(file.FileName),
                        SampleName = parsed.SampleName,
                        InstrumentModel = parsed.InstrumentModel,
                        Sequence = parsed.Sequence,
                        SequenceLength = parsed.Sequence.Length,
                        Direction = DetectDirection(file.FileName),
                        Notes = $"ABIF {parsed.Version / 100.0:F2}, QV {parsed.QualityValues.Count}",
                        CreatedAt = DateTime.UtcNow,
                        ProjectId = projectId,
                        BaseOrder  =  parsed.BaseOrder,
                        QualityValuesJson = JsonSerializer.Serialize(parsed.QualityValues),
                        TraceDataJson = JsonSerializer.Serialize(parsed.Traces),
                    };

                    var created = await _readStorage.Insert(readModel);
                    if (created != null)
                    {
                        _logger.LogInformation($"Read file uploaded successfully, projectID={projectId}, fileName={file.FileName}");
                        uploaded.Add(created);
                    }
                }

                _logger.LogInformation($"Upload reads success, projectID={projectId}");
                return uploaded;
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning($"Upload reads failed, wrong data format, projectID={projectId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Upload reads failed, unexpected error, projectID={projectId}");
                throw;
            }
        }

        public async Task<List<ReadModel>> GetProjectReadsAsync(int projectId)
        {
            _logger.LogInformation($"Get project reads request received, projectID={projectId}");

            try
            {
                var result = await _readStorage.GetFilteredList(new ReadSearchModel { ProjectId = projectId });

                _logger.LogInformation($"Get project reads success, projectID={projectId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get project reads failed, unexpected error, projectID={projectId}");
                throw;
            }
        }

        public async Task<ReadModel?> ReadElement(ReadSearchModel model)
        {
            _logger.LogInformation($"Read element request received, readID={model?.Id}, projectID={model?.ProjectId}");

            try
            {
                if (model == null)
                {
                    _logger.LogWarning($"Read element failed, search model is null");
                    throw new ArgumentNullException(nameof(model));
                }

                var element = await _readStorage.GetElement(model);

                if (element == null)
                {
                    _logger.LogWarning($"Read element failed, read not found, readID={model.Id}, projectID={model.ProjectId}");
                    return null;
                }

                _logger.LogInformation($"Read element success, readID={element.Id}, projectID={element.ProjectId}");
                return element;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning($"Read element failed, search model is null");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Read element failed, unexpected error, readID={model?.Id}, projectID={model?.ProjectId}");
                throw;
            }
        }

        public async Task<bool> Delete(int id)
        {
            _logger.LogInformation($"Delete read request received, readID={id}");

            try
            {
                var result = await _readStorage.Delete(id);

                if (result == null)
                {
                    _logger.LogWarning($"Delete read failed, read not found, readID={id}");
                    return false;
                }

                _logger.LogInformation($"Delete read success, readID={id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete read failed, unexpected error, readID={id}");
                throw;
            }
        }

        private static ReadDirectionEnum DetectDirection(string fileName)
        {
            var normalized = fileName.ToUpperInvariant();

            if (normalized.Contains("_F") || normalized.Contains("FORWARD"))
            {
                return ReadDirectionEnum.Forward;
            }

            if (normalized.Contains("_R") || normalized.Contains("REVERSE"))
            {
                return ReadDirectionEnum.Reverse;
            }

            return ReadDirectionEnum.Unknown;
        }
    }
}