using System.Buffers.Binary;
using System.Text;

namespace Web_prototype
{
    public static class Ab1Parser
    {
        public static Ab1ReadResult Parse(byte[] buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.Length < 128)
            {
                throw new InvalidDataException("Файл слишком короткий для AB1/ABIF.");
            }

            var signature = Encoding.ASCII.GetString(buffer, 0, 4);
            if (!string.Equals(signature, "ABIF", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Файл не является AB1/ABIF.");
            }

            var version = ReadUInt16BE(buffer, 4);
            var rootEntry = ReadDirectoryEntry(buffer, 6);

            if (!string.Equals(rootEntry.TagName, "tdir", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Не найден корневой каталог ABIF (tdir).");
            }

            var entries = ReadAllDirectoryEntries(buffer, rootEntry);

            var sequence = ExtractSequence(entries, buffer);

            var qualityValues =
                GetBytes(entries, buffer, "PCON", 2) ??
                GetBytes(entries, buffer, "PCON", 1) ??
                Array.Empty<byte>();

            var baseOrder = GetText(entries, buffer, "FWO_", 1);

            var sampleName =
                GetText(entries, buffer, "SMPL", 1) ??
                GetText(entries, buffer, "TUBE", 1);

            var instrumentModel =
                GetText(entries, buffer, "MCHN", 1) ??
                GetText(entries, buffer, "MODL", 1) ??
                GetText(entries, buffer, "MODF", 1) ??
                GetText(entries, buffer, "MTXF", 1);

            var traces = new Dictionary<string, short[]>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < 4; i++)
            {
                var tagNumber = 9u + (uint)i;
                var entry = FindEntry(entries, "DATA", tagNumber);
                if (entry is null)
                {
                    continue;
                }

                var values = GetInt16Array(buffer, entry);

                var channelName = baseOrder is { Length: >= 4 }
                    ? baseOrder[i].ToString()
                    : $"DATA{tagNumber}";

                traces[channelName] = values;
            }

            return new Ab1ReadResult
            {
                Signature = signature,
                Version = version,
                RootDirectoryEntry = rootEntry,
                Entries = entries,
                Sequence = sequence,
                QualityValues = qualityValues,
                BaseOrder = baseOrder,
                SampleName = sampleName,
                InstrumentModel = instrumentModel,
                Traces = traces
            };
        }

        public static string ExtractSequence(byte[] buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.Length < 128)
            {
                throw new InvalidDataException("Файл слишком короткий для AB1/ABIF.");
            }

            var signature = Encoding.ASCII.GetString(buffer, 0, 4);
            if (!string.Equals(signature, "ABIF", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Файл не является AB1/ABIF.");
            }

            var rootEntry = ReadDirectoryEntry(buffer, 6);

            if (!string.Equals(rootEntry.TagName, "tdir", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Не найден корневой каталог ABIF (tdir).");
            }

            var entries = ReadAllDirectoryEntries(buffer, rootEntry);
            return ExtractSequence(entries, buffer);
        }

        private static string ExtractSequence(IReadOnlyList<Ab1DirectoryEntry> entries, byte[] buffer)
        {
            var entry =
                FindEntry(entries, "PBAS", 2) ??
                FindEntry(entries, "PBAS", 1);

            if (entry is null)
            {
                throw new InvalidDataException("В файле не найден тег PBAS с последовательностью.");
            }

            var data = ReadEntryData(buffer, entry);
            return Encoding.ASCII.GetString(data).TrimEnd('\0', ' ', '\r', '\n');
        }

        private static List<Ab1DirectoryEntry> ReadAllDirectoryEntries(byte[] buffer, Ab1DirectoryEntry rootEntry)
        {
            checked
            {
                var directoryOffset = (int)rootEntry.DataOffset;
                var directoryCount = (int)rootEntry.ElementCount;

                var entries = new List<Ab1DirectoryEntry>(directoryCount);

                for (var i = 0; i < directoryCount; i++)
                {
                    var entryOffset = directoryOffset + (i * 28);
                    entries.Add(ReadDirectoryEntry(buffer, entryOffset));
                }

                return entries;
            }
        }

        private static Ab1DirectoryEntry ReadDirectoryEntry(byte[] buffer, int offset)
        {
            if (offset < 0 || offset + 28 > buffer.Length)
            {
                throw new InvalidDataException("Некорректное смещение записи каталога ABIF.");
            }

            return new Ab1DirectoryEntry
            {
                TagName = Encoding.ASCII.GetString(buffer, offset, 4),
                TagNumber = ReadUInt32BE(buffer, offset + 4),
                ElementType = ReadUInt16BE(buffer, offset + 8),
                ElementSize = ReadUInt16BE(buffer, offset + 10),
                ElementCount = ReadUInt32BE(buffer, offset + 12),
                DataSize = ReadUInt32BE(buffer, offset + 16),
                DataOffset = ReadUInt32BE(buffer, offset + 20),
                DataHandle = ReadUInt32BE(buffer, offset + 24)
            };
        }

        private static Ab1DirectoryEntry? FindEntry(
            IEnumerable<Ab1DirectoryEntry> entries,
            string tagName,
            uint tagNumber)
        {
            return entries.FirstOrDefault(e =>
                string.Equals(e.TagName, tagName, StringComparison.Ordinal) &&
                e.TagNumber == tagNumber);
        }

        private static string? GetText(
            IEnumerable<Ab1DirectoryEntry> entries,
            byte[] buffer,
            string tagName,
            uint tagNumber)
        {
            var entry = FindEntry(entries, tagName, tagNumber);
            if (entry is null)
            {
                return null;
            }

            var bytes = ReadEntryData(buffer, entry);
            if (bytes.Length == 0)
            {
                return null;
            }

            return DecodeText(bytes);
        }

        private static byte[]? GetBytes(
            IEnumerable<Ab1DirectoryEntry> entries,
            byte[] buffer,
            string tagName,
            uint tagNumber)
        {
            var entry = FindEntry(entries, tagName, tagNumber);
            return entry is null ? null : ReadEntryData(buffer, entry);
        }

        private static short[] GetInt16Array(byte[] buffer, Ab1DirectoryEntry entry)
        {
            var bytes = ReadEntryData(buffer, entry);

            if (bytes.Length % 2 != 0)
            {
                throw new InvalidDataException(
                    $"Тег {entry.TagName}{entry.TagNumber} содержит нечетное число байт для Int16.");
            }

            var result = new short[bytes.Length / 2];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = ReadInt16BE(bytes, i * 2);
            }

            return result;
        }

        private static byte[] ReadEntryData(byte[] buffer, Ab1DirectoryEntry entry)
        {
            checked
            {
                var dataSize = (int)entry.DataSize;

                if (dataSize < 0)
                {
                    throw new InvalidDataException("Некорректный размер данных в ABIF.");
                }

                // Если данных 4 байта или меньше, ABIF может хранить их прямо в поле DataOffset.
                if (dataSize <= 4)
                {
                    var inline = new byte[4];
                    BinaryPrimitives.WriteUInt32BigEndian(inline, entry.DataOffset);
                    return inline.Take(dataSize).ToArray();
                }

                var dataOffset = (int)entry.DataOffset;

                if (dataOffset < 0 || dataOffset + dataSize > buffer.Length)
                {
                    throw new InvalidDataException(
                        $"Данные тега {entry.TagName}{entry.TagNumber} выходят за пределы файла.");
                }

                return buffer.AsSpan(dataOffset, dataSize).ToArray();
            }
        }

        private static string DecodeText(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            // Некоторые строки в ABIF хранятся как Pascal string: первый байт = длина строки.
            var pascalLength = bytes[0];

            if (pascalLength > 0 && pascalLength <= bytes.Length - 1)
            {
                var pascalText = Encoding.ASCII.GetString(bytes, 1, pascalLength).TrimEnd('\0');

                if (!string.IsNullOrWhiteSpace(pascalText))
                {
                    return pascalText;
                }
            }

            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }

        private static ushort ReadUInt16BE(byte[] buffer, int offset)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
        }

        private static uint ReadUInt32BE(byte[] buffer, int offset)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
        }

        private static short ReadInt16BE(byte[] buffer, int offset)
        {
            return BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(offset, 2));
        }
    }

    public sealed class Ab1ReadResult
    {
        public string Signature { get; init; } = string.Empty;
        public ushort Version { get; init; }
        public Ab1DirectoryEntry RootDirectoryEntry { get; init; } = new();
        public IReadOnlyList<Ab1DirectoryEntry> Entries { get; init; } = Array.Empty<Ab1DirectoryEntry>();

        public string Sequence { get; init; } = string.Empty;
        public IReadOnlyList<byte> QualityValues { get; init; } = Array.Empty<byte>();
        public string? BaseOrder { get; init; }
        public string? SampleName { get; init; }
        public string? InstrumentModel { get; init; }
        public IReadOnlyDictionary<string, short[]> Traces { get; init; }
            = new Dictionary<string, short[]>();
    }

    public sealed class Ab1DirectoryEntry
    {
        public string TagName { get; init; } = string.Empty;
        public uint TagNumber { get; init; }
        public ushort ElementType { get; init; }
        public ushort ElementSize { get; init; }
        public uint ElementCount { get; init; }
        public uint DataSize { get; init; }
        public uint DataOffset { get; init; }
        public uint DataHandle { get; init; }
    }
}
