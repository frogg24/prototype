using BusinessLogic;
using DataModels.enums;
using DataModels.Interfaces;
using DataModels.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Buffers.Binary;
using System.Text;

namespace Tests
{
    [TestClass]
    public class ReadLogicTests
    {
        private static ReadLogic CreateLogic(Mock<IReadStorage> storageMock)
        {
            return new ReadLogic(
                storageMock.Object,
                NullLogger<ReadLogic>.Instance
            );
        }

        private static ReadModel CreateValidRead()
        {
            return new ReadModel
            {
                Id = 1,
                FileName = "test_F.ab1",
                SampleName = "sample",
                InstrumentModel = "instrument",
                Sequence = "ACGT",
                SequenceLength = 4,
                Direction = ReadDirectionEnum.Forward,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow,
                QualityValuesJson = "[30,31,32,33]",
                TraceDataJson = "{}"
            };
        }

        [TestMethod]
        public async Task UploadReadsAsync_FileContentIsEmpty_ShouldReturnEmptyList()
        {
            var storageMock = new Mock<IReadStorage>();
            var logic = CreateLogic(storageMock);

            var files = new List<UploadReadFileModel>
            {
                new UploadReadFileModel
                {
                    FileName = "test.ab1",
                    Content = Array.Empty<byte>()
                }
            };

            var result = await logic.UploadReadsAsync(1, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            storageMock.Verify(x => x.Insert(It.IsAny<ReadModel>()), Times.Never);
        }

        [TestMethod]
        public async Task UploadReadsAsync_FileIsNotAb1_ShouldThrowInvalidDataException()
        {
            var storageMock = new Mock<IReadStorage>();
            var logic = CreateLogic(storageMock);

            var files = new List<UploadReadFileModel>
            {
                new UploadReadFileModel
                {
                    FileName = "test.txt",
                    Content = new byte[] { 1, 2, 3 }
                }
            };

            await Assert.ThrowsExceptionAsync<InvalidDataException>(async () =>
                await logic.UploadReadsAsync(1, files));

            storageMock.Verify(x => x.Insert(It.IsAny<ReadModel>()), Times.Never);
        }

        [TestMethod]
        public async Task UploadReadsAsync_InvalidAb1Content_ShouldThrowInvalidDataException()
        {
            var storageMock = new Mock<IReadStorage>();
            var logic = CreateLogic(storageMock);

            var files = new List<UploadReadFileModel>
            {
                new UploadReadFileModel
                {
                    FileName = "test.ab1",
                    Content = new byte[] { 1, 2, 3 }
                }
            };

            await Assert.ThrowsExceptionAsync<InvalidDataException>(async () =>
                await logic.UploadReadsAsync(1, files));

            storageMock.Verify(x => x.Insert(It.IsAny<ReadModel>()), Times.Never);
        }

        [TestMethod]
        public async Task UploadReadsAsync_ValidAb1File_ShouldInsertAndReturnUploadedReads()
        {
            var storageMock = new Mock<IReadStorage>();

            ReadModel? insertedModel = null;

            storageMock
                .Setup(x => x.Insert(It.IsAny<ReadModel>()))
                .Callback<ReadModel>(model => insertedModel = model)
                .Returns((ReadModel model) => Task.FromResult<ReadModel?>(model));

            var logic = CreateLogic(storageMock);

            var files = new List<UploadReadFileModel>
            {
                new UploadReadFileModel
                {
                    FileName = "test_F.ab1",
                    Content = CreateMinimalAb1Buffer()
                }
            };

            var result = await logic.UploadReadsAsync(1, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            Assert.IsNotNull(insertedModel);
            Assert.AreEqual("test_F.ab1", insertedModel.FileName);
            Assert.AreEqual("ACGT", insertedModel.Sequence);
            Assert.AreEqual(4, insertedModel.SequenceLength);
            Assert.AreEqual(1, insertedModel.ProjectId);
            Assert.AreEqual(ReadDirectionEnum.Forward, insertedModel.Direction);
            Assert.IsFalse(string.IsNullOrWhiteSpace(insertedModel.QualityValuesJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(insertedModel.TraceDataJson));

            storageMock.Verify(x => x.Insert(It.IsAny<ReadModel>()), Times.Once);
        }

        [TestMethod]
        public async Task UploadReadsAsync_StorageReturnsNull_ShouldReturnEmptyList()
        {
            var storageMock = new Mock<IReadStorage>();

            storageMock
                .Setup(x => x.Insert(It.IsAny<ReadModel>()))
                .ReturnsAsync((ReadModel?)null);

            var logic = CreateLogic(storageMock);

            var files = new List<UploadReadFileModel>
            {
                new UploadReadFileModel
                {
                    FileName = "test_F.ab1",
                    Content = CreateMinimalAb1Buffer()
                }
            };

            var result = await logic.UploadReadsAsync(1, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            storageMock.Verify(x => x.Insert(It.IsAny<ReadModel>()), Times.Once);
        }

        [TestMethod]
        public async Task GetProjectReadsAsync_ShouldReturnProjectReads()
        {
            var storageMock = new Mock<IReadStorage>();

            var reads = new List<ReadModel>
            {
                CreateValidRead()
            };

            storageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == 1)))
                .ReturnsAsync(reads);

            var logic = CreateLogic(storageMock);

            var result = await logic.GetProjectReadsAsync(1);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].ProjectId);

            storageMock.Verify(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == 1)), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IReadStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.ReadElement(null!));

            storageMock.Verify(x => x.GetElement(It.IsAny<ReadSearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadElement_ReadExists_ShouldReturnRead()
        {
            var storageMock = new Mock<IReadStorage>();

            var searchModel = new ReadSearchModel
            {
                Id = 1
            };

            var read = CreateValidRead();

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync(read);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(read.Id, result.Id);
            Assert.AreEqual(read.Sequence, result.Sequence);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ReadDoesNotExist_ShouldReturnNull()
        {
            var storageMock = new Mock<IReadStorage>();

            var searchModel = new ReadSearchModel
            {
                Id = 1
            };

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync((ReadModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task Delete_ReadExists_ShouldReturnTrue()
        {
            var storageMock = new Mock<IReadStorage>();

            var read = CreateValidRead();

            storageMock
                .Setup(x => x.Delete(read.Id))
                .ReturnsAsync(read);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(read.Id);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Delete(read.Id), Times.Once);
        }

        [TestMethod]
        public async Task Delete_ReadDoesNotExist_ShouldReturnFalse()
        {
            var storageMock = new Mock<IReadStorage>();

            storageMock
                .Setup(x => x.Delete(1))
                .ReturnsAsync((ReadModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(1);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Delete(1), Times.Once);
        }

        private static byte[] CreateMinimalAb1Buffer()
        {
            var buffer = new byte[128];

            Encoding.ASCII.GetBytes("ABIF").CopyTo(buffer, 0);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 101);

            WriteDirectoryEntry(
                buffer,
                offset: 6,
                tagName: "tdir",
                tagNumber: 1,
                elementType: 0,
                elementSize: 0,
                elementCount: 2,
                dataSize: 56,
                dataOffset: 40,
                dataHandle: 0
            );

            WriteDirectoryEntry(
                buffer,
                offset: 40,
                tagName: "PBAS",
                tagNumber: 2,
                elementType: 0,
                elementSize: 1,
                elementCount: 4,
                dataSize: 4,
                dataOffset: InlineBytes("ACGT"),
                dataHandle: 0
            );

            WriteDirectoryEntry(
                buffer,
                offset: 68,
                tagName: "PCON",
                tagNumber: 2,
                elementType: 0,
                elementSize: 1,
                elementCount: 4,
                dataSize: 4,
                dataOffset: InlineBytes(new byte[] { 30, 31, 32, 33 }),
                dataHandle: 0
            );

            return buffer;
        }

        private static void WriteDirectoryEntry(
            byte[] buffer,
            int offset,
            string tagName,
            uint tagNumber,
            ushort elementType,
            ushort elementSize,
            uint elementCount,
            uint dataSize,
            uint dataOffset,
            uint dataHandle)
        {
            Encoding.ASCII.GetBytes(tagName).CopyTo(buffer, offset);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 4, 4), tagNumber);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 8, 2), elementType);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 10, 2), elementSize);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 12, 4), elementCount);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 16, 4), dataSize);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 20, 4), dataOffset);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 24, 4), dataHandle);
        }

        private static uint InlineBytes(string text)
        {
            return InlineBytes(Encoding.ASCII.GetBytes(text));
        }

        private static uint InlineBytes(byte[] bytes)
        {
            var inline = new byte[4];
            Array.Copy(bytes, inline, Math.Min(bytes.Length, 4));
            return BinaryPrimitives.ReadUInt32BigEndian(inline);
        }
    }
}