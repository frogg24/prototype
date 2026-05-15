using BusinessLogic;
using DataModels.Interfaces;
using DataModels.ProjectModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests
{
    [TestClass]
    public class ProjectLogicTests
    {
        private static ProjectLogic CreateLogic(Mock<IProjectStorage> storageMock)
        {
            return new ProjectLogic(
                storageMock.Object,
                NullLogger<ProjectLogic>.Instance
            );
        }

        private static ProjectModel CreateValidProject()
        {
            return new ProjectModel
            {
                Id = 1,
                UserId = 10,
                Title = "Test project",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        [TestMethod]
        public async Task ReadList_ModelIsNull_ShouldReturnFullList()
        {
            var storageMock = new Mock<IProjectStorage>();

            var projects = new List<ProjectModel>
            {
                CreateValidProject()
            };

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync(projects);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Test project", result[0].Title);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
            storageMock.Verify(x => x.GetFilteredList(It.IsAny<ProjectSearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_ModelIsNotNull_ShouldReturnFilteredList()
        {
            var storageMock = new Mock<IProjectStorage>();

            var searchModel = new ProjectSearchModel
            {
                UserId = 10
            };

            var projects = new List<ProjectModel>
            {
                CreateValidProject()
            };

            storageMock
                .Setup(x => x.GetFilteredList(searchModel))
                .ReturnsAsync(projects);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(10, result[0].UserId);

            storageMock.Verify(x => x.GetFilteredList(searchModel), Times.Once);
            storageMock.Verify(x => x.GetFullList(), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_StorageReturnsNull_ShouldReturnNull()
        {
            var storageMock = new Mock<IProjectStorage>();

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync((List<ProjectModel>)null!);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.ReadElement(null!));

            storageMock.Verify(x => x.GetElement(It.IsAny<ProjectSearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadElement_ProjectExists_ShouldReturnProject()
        {
            var storageMock = new Mock<IProjectStorage>();

            var searchModel = new ProjectSearchModel
            {
                Id = 1
            };

            var project = CreateValidProject();

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync(project);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(project.Id, result.Id);
            Assert.AreEqual(project.Title, result.Title);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ProjectDoesNotExist_ShouldReturnNull()
        {
            var storageMock = new Mock<IProjectStorage>();

            var searchModel = new ProjectSearchModel
            {
                Id = 1
            };

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync((ProjectModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task Create_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Create(null!));

            storageMock.Verify(x => x.Insert(It.IsAny<ProjectModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_UserIdIsZero_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidProject();
            model.UserId = 0;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<ProjectModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_UserIdIsNegative_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidProject();
            model.UserId = -1;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<ProjectModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IProjectStorage>();

            var model = CreateValidProject();

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
            var storageMock = new Mock<IProjectStorage>();

            var model = CreateValidProject();

            storageMock
                .Setup(x => x.Insert(model))
                .ReturnsAsync((ProjectModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Create(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Insert(model), Times.Once);
        }

        [TestMethod]
        public async Task Update_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Update(null!));

            storageMock.Verify(x => x.Update(It.IsAny<ProjectModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_UserIdIsZero_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IProjectStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidProject();
            model.UserId = 0;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<ProjectModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IProjectStorage>();

            var model = CreateValidProject();

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
            var storageMock = new Mock<IProjectStorage>();

            var model = CreateValidProject();

            storageMock
                .Setup(x => x.Update(model))
                .ReturnsAsync((ProjectModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Update(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Update(model), Times.Once);
        }

        [TestMethod]
        public async Task Delete_ProjectExists_ShouldReturnTrue()
        {
            var storageMock = new Mock<IProjectStorage>();

            var project = CreateValidProject();

            storageMock
                .Setup(x => x.Delete(project.Id))
                .ReturnsAsync(project);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(project.Id);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Delete(project.Id), Times.Once);
        }

        [TestMethod]
        public async Task Delete_ProjectDoesNotExist_ShouldReturnFalse()
        {
            var storageMock = new Mock<IProjectStorage>();

            storageMock
                .Setup(x => x.Delete(1))
                .ReturnsAsync((ProjectModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(1);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Delete(1), Times.Once);
        }
    }
}