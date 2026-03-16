using DataModels;
using DataModels.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Read
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? SampleName { get; set; }

        [MaxLength(255)]
        public string? InstrumentModel { get; set; }

        [Required]
        public string Sequence { get; set; } = string.Empty;

        public int SequenceLength { get; set; }

        public ReadDirectionEnum Direction { get; set; } = ReadDirectionEnum.Unknown;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }

        public static Read? Create(ReadModel read)
        {
            if (read == null)
            {
                return null;
            }
            return new Read
            {
                Id = read.Id,
                FileName = read.FileName,
                SampleName = read.SampleName,
                InstrumentModel = read.InstrumentModel,
                Sequence = read.Sequence,
                SequenceLength = read.Sequence.Length,
                Direction = read.Direction,
                Notes = read.Notes,
                CreatedAt = read.CreatedAt,
                UserId = read.UserId,
            };
        }

        public void Update(ReadModel read)
        {
            if (read == null)
            {
                return;
            }

            FileName = read.FileName;
            SampleName = read.SampleName;
            InstrumentModel = read.InstrumentModel;
            Sequence = read.Sequence;
            SequenceLength = read.Sequence.Length;
            Direction = read.Direction;
            Notes = read.Notes;
            UserId = read.UserId;
        }

        public ReadModel GetViewModel => new()
        {
            Id = Id,
            FileName = FileName,
            SampleName = SampleName,
            InstrumentModel = InstrumentModel,
            Sequence = Sequence,
            SequenceLength = SequenceLength,
            Direction = Direction,
            Notes = Notes,
            CreatedAt = CreatedAt,
            UserId = UserId,
        };
    }
}
