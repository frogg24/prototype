using DataModels.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class ReadModel
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? SampleName { get; set; }

        [MaxLength(255)]
        public string? InstrumentModel { get; set; }

        [MaxLength(10)]
        public string? BaseOrder { get; set; }

        [Required]
        public string Sequence { get; set; } = string.Empty;

        public int SequenceLength { get; set; }

        // Forward / Reverse / Unknown
        public ReadDirectionEnum Direction { get; set; } = ReadDirectionEnum.Unknown;

        // Если захочешь потом использовать quality values
        public string? QualityValuesJson { get; set; }

        // Если захочешь потом рисовать хроматограмму
        public string? TraceDataJson { get; set; }

        //// Хранить ли сам исходный .ab1 в БД
        //public byte[]? RawFileContent { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt{ get; set; } = DateTime.UtcNow;
        public int? ProjectId { get; set; }
    }
}
