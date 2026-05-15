using BusinessLogic;
using DataModels.AssemblyModels;
using DataModels.Interfaces;
using DataModels.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;

namespace Tests
{
    [TestClass]
    public class AssemblyLogicTests
    {
        private static AssemblyLogic CreateLogic(
            Mock<IAssemblyStorage> assemblyStorageMock,
            Mock<IReadStorage>? readStorageMock = null)
        {
            readStorageMock ??= new Mock<IReadStorage>();

            var algorithm = new AlgorithmOLC(
                readStorageMock.Object,
                NullLogger<AlgorithmOLC>.Instance
            );

            return new AssemblyLogic(
                assemblyStorageMock.Object,
                algorithm,
                NullLogger<AssemblyLogic>.Instance
            );
        }

        private static AssemblyModel CreateValidAssembly()
        {
            return new AssemblyModel
            {
                Id = 1,
                ProjectId = 10,
                ConsensusSequence = "ACGT",
                ConsensusLength = 4,
                QualityValuesJson = "[30,31,32,33]",
                TraceDataJson = "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static List<ReadModel> CreateReadsForOlc(int projectId)
        {
            var sequence1 = new string('A', 20) + new string('C', 20);
            var sequence2 = new string('C', 20) + new string('G', 20);

            return new List<ReadModel>
            {
                new ReadModel
                {
                    Id = 1,
                    ProjectId = projectId,
                    FileName = "read1_F.ab1",
                    Sequence = sequence1,
                    SequenceLength = sequence1.Length,
                    QualityValuesJson = JsonSerializer.Serialize(Enumerable.Repeat(30, sequence1.Length).ToList()),
                    CreatedAt = DateTime.UtcNow
                },
                new ReadModel
                {
                    Id = 2,
                    ProjectId = projectId,
                    FileName = "read2_F.ab1",
                    Sequence = sequence2,
                    SequenceLength = sequence2.Length,
                    QualityValuesJson = JsonSerializer.Serialize(Enumerable.Repeat(30, sequence2.Length).ToList()),
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        [TestMethod]
        public async Task MakeOLC_ProjectIdIsZero_ShouldThrowArgumentException()
        {
            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(assemblyStorageMock);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.MakeOLC(0));

            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task MakeOLC_ProjectIdIsNegative_ShouldThrowArgumentException()
        {
            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(assemblyStorageMock);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.MakeOLC(-1));

            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task MakeOLC_NoExistingAssembly_ShouldInsertAndReturnCreatedAssembly()
        {
            var projectId = 10;

            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var readStorageMock = new Mock<IReadStorage>();

            readStorageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(CreateReadsForOlc(projectId));

            assemblyStorageMock
                .Setup(x => x.GetElement(It.Is<AssemblySearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync((AssemblyModel?)null);

            assemblyStorageMock
                .Setup(x => x.Insert(It.IsAny<AssemblyModel>()))
                .ReturnsAsync((AssemblyModel model) => model);

            var logic = CreateLogic(assemblyStorageMock, readStorageMock);

            var result = await logic.MakeOLC(projectId);

            Assert.IsNotNull(result);
            Assert.AreEqual(projectId, result.ProjectId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ConsensusSequence));
            Assert.AreEqual(result.ConsensusSequence.Length, result.ConsensusLength);

            readStorageMock.Verify(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)), Times.Once);
            assemblyStorageMock.Verify(x => x.GetElement(It.Is<AssemblySearchModel>(m => m.ProjectId == projectId)), Times.Once);
            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Once);
            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task MakeOLC_ExistingAssembly_ShouldUpdateAndReturnUpdatedAssembly()
        {
            var projectId = 10;

            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var readStorageMock = new Mock<IReadStorage>();

            var existingAssembly = CreateValidAssembly();
            existingAssembly.Id = 5;
            existingAssembly.ProjectId = projectId;

            readStorageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(CreateReadsForOlc(projectId));

            assemblyStorageMock
                .Setup(x => x.GetElement(It.Is<AssemblySearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(existingAssembly);

            assemblyStorageMock
                .Setup(x => x.Update(It.IsAny<AssemblyModel>()))
                .ReturnsAsync((AssemblyModel model) => model);

            var logic = CreateLogic(assemblyStorageMock, readStorageMock);

            var result = await logic.MakeOLC(projectId);

            Assert.IsNotNull(result);
            Assert.AreEqual(existingAssembly.Id, result.Id);
            Assert.AreEqual(projectId, result.ProjectId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ConsensusSequence));
            Assert.AreEqual(result.ConsensusSequence.Length, result.ConsensusLength);

            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Once);
        }

        [TestMethod]
        public async Task MakeOLC_InsertReturnsNull_ShouldThrowInvalidOperationException()
        {
            var projectId = 10;

            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var readStorageMock = new Mock<IReadStorage>();

            readStorageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(CreateReadsForOlc(projectId));

            assemblyStorageMock
                .Setup(x => x.GetElement(It.Is<AssemblySearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync((AssemblyModel?)null);

            assemblyStorageMock
                .Setup(x => x.Insert(It.IsAny<AssemblyModel>()))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(assemblyStorageMock, readStorageMock);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await logic.MakeOLC(projectId));

            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Once);
        }

        [TestMethod]
        public async Task MakeOLC_UpdateReturnsNull_ShouldThrowInvalidOperationException()
        {
            var projectId = 10;

            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var readStorageMock = new Mock<IReadStorage>();

            var existingAssembly = CreateValidAssembly();
            existingAssembly.ProjectId = projectId;

            readStorageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(CreateReadsForOlc(projectId));

            assemblyStorageMock
                .Setup(x => x.GetElement(It.Is<AssemblySearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(existingAssembly);

            assemblyStorageMock
                .Setup(x => x.Update(It.IsAny<AssemblyModel>()))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(assemblyStorageMock, readStorageMock);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await logic.MakeOLC(projectId));

            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Once);
        }

        [TestMethod]
        public async Task MakeOLC_NotEnoughReads_ShouldThrowException()
        {
            var projectId = 10;

            var assemblyStorageMock = new Mock<IAssemblyStorage>();
            var readStorageMock = new Mock<IReadStorage>();

            readStorageMock
                .Setup(x => x.GetFilteredList(It.Is<ReadSearchModel>(m => m.ProjectId == projectId)))
                .ReturnsAsync(CreateReadsForOlc(projectId).Take(1).ToList());

            var logic = CreateLogic(assemblyStorageMock, readStorageMock);

            await Assert.ThrowsExceptionAsync<Exception>(async () =>
                await logic.MakeOLC(projectId));

            assemblyStorageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
            assemblyStorageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task GetProjectAssemblysAsync_ShouldReturnProjectAssemblies()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var assemblies = new List<AssemblyModel>
            {
                CreateValidAssembly()
            };

            storageMock
                .Setup(x => x.GetFilteredList(It.Is<AssemblySearchModel>(m => m.ProjectId == 10)))
                .ReturnsAsync(assemblies);

            var logic = CreateLogic(storageMock);

            var result = await logic.GetProjectAssemblysAsync(10);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(10, result[0].ProjectId);

            storageMock.Verify(x => x.GetFilteredList(It.Is<AssemblySearchModel>(m => m.ProjectId == 10)), Times.Once);
        }

        [TestMethod]
        public async Task ReadList_ModelIsNull_ShouldReturnFullList()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var assemblies = new List<AssemblyModel>
            {
                CreateValidAssembly()
            };

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync(assemblies);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
            storageMock.Verify(x => x.GetFilteredList(It.IsAny<AssemblySearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_ModelIsNotNull_ShouldReturnFilteredList()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var searchModel = new AssemblySearchModel
            {
                ProjectId = 10
            };

            var assemblies = new List<AssemblyModel>
            {
                CreateValidAssembly()
            };

            storageMock
                .Setup(x => x.GetFilteredList(searchModel))
                .ReturnsAsync(assemblies);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            storageMock.Verify(x => x.GetFilteredList(searchModel), Times.Once);
            storageMock.Verify(x => x.GetFullList(), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_StorageReturnsNull_ShouldReturnNull()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync((List<AssemblyModel>)null!);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.ReadElement(null!));

            storageMock.Verify(x => x.GetElement(It.IsAny<AssemblySearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadElement_AssemblyExists_ShouldReturnAssembly()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var searchModel = new AssemblySearchModel
            {
                Id = 1
            };

            var assembly = CreateValidAssembly();

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync(assembly);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(assembly.Id, result.Id);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_AssemblyDoesNotExist_ShouldReturnNull()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var searchModel = new AssemblySearchModel
            {
                Id = 1
            };

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task Create_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Create(null!));

            storageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_ProjectIdIsZero_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidAssembly();
            model.ProjectId = 0;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var model = CreateValidAssembly();

            storageMock
                .Setup(x => x.Insert(model))
                .ReturnsAsync(model);

            var logic = CreateLogic(storageMock);

            var result = await logic.Create(model);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Insert(model), Times.Once);
        }

        [TestMethod]
        public async Task Create_StorageReturnsNull_ShouldReturnFalse()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var model = CreateValidAssembly();

            storageMock
                .Setup(x => x.Insert(model))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Create(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Insert(model), Times.Once);
        }

        [TestMethod]
        public async Task Update_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Update(null!));

            storageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_ProjectIdIsZero_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IAssemblyStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidAssembly();
            model.ProjectId = 0;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<AssemblyModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var model = CreateValidAssembly();

            storageMock
                .Setup(x => x.Update(model))
                .ReturnsAsync(model);

            var logic = CreateLogic(storageMock);

            var result = await logic.Update(model);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Update(model), Times.Once);
        }

        [TestMethod]
        public async Task Update_StorageReturnsNull_ShouldReturnFalse()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var model = CreateValidAssembly();

            storageMock
                .Setup(x => x.Update(model))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Update(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Update(model), Times.Once);
        }

        [TestMethod]
        public async Task Delete_AssemblyExists_ShouldReturnTrue()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            var assembly = CreateValidAssembly();

            storageMock
                .Setup(x => x.Delete(assembly.Id))
                .ReturnsAsync(assembly);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(assembly.Id);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Delete(assembly.Id), Times.Once);
        }

        [TestMethod]
        public async Task Delete_AssemblyDoesNotExist_ShouldReturnFalse()
        {
            var storageMock = new Mock<IAssemblyStorage>();

            storageMock
                .Setup(x => x.Delete(1))
                .ReturnsAsync((AssemblyModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(1);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Delete(1), Times.Once);
        }
    }
}