using Database.Implements;
using DataModels;
using DataModels.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ReadLogic
    {
        private readonly ReadStorage _readStorage;

        public ReadLogic(ReadStorage readStorage)
        {
            _readStorage = readStorage;
        }

        public async Task<List<ReadModel>> UploadReadsAsync(int? userId, IEnumerable<UploadReadFileModel> files)
        {
            var uploaded = new List<ReadModel>();

            foreach (var file in files)
            {
                if (file.Content.Length == 0)
                {
                    continue;
                }

                if (!string.Equals(Path.GetExtension(file.FileName), ".ab1", StringComparison.OrdinalIgnoreCase))
                {
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
                    UserId = userId,
                };

                var created = await _readStorage.Insert(readModel);
                if (created != null)
                {
                    uploaded.Add(created);
                }
            }

            return uploaded;
        }

        public async Task<List<ReadModel>> GetUserReadsAsync(int userId)
        {
            return await _readStorage.GetFilteredList(new ReadSearchModel { UserId = userId });
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
