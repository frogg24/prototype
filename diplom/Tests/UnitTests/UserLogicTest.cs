using BusinessLogic;
using DataModels.Interfaces;
using DataModels.UserModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests
{
    [TestClass]
    public class UserLogicTests
    {
        private static UserLogic CreateLogic(Mock<IUserStorage> storageMock)
        {
            return new UserLogic(
                storageMock.Object,
                NullLogger<UserLogic>.Instance
            );
        }

        private static UserModel CreateValidUser()
        {
            return new UserModel
            {
                Id = 1,
                Username = "test",
                Email = "test@test.ru",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };
        }

        [TestMethod]
        public async Task ReadList_ModelIsNull_ShouldReturnFullList()
        {
            var storageMock = new Mock<IUserStorage>();

            var users = new List<UserModel>
            {
                CreateValidUser()
            };

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync(users);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("test@test.ru", result[0].Email);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
            storageMock.Verify(x => x.GetFilteredList(It.IsAny<UserSearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_ModelIsNotNull_ShouldReturnFilteredList()
        {
            var storageMock = new Mock<IUserStorage>();

            var searchModel = new UserSearchModel
            {
                Email = "test@test.ru"
            };

            var users = new List<UserModel>
            {
                CreateValidUser()
            };

            storageMock
                .Setup(x => x.GetFilteredList(searchModel))
                .ReturnsAsync(users);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("test@test.ru", result[0].Email);

            storageMock.Verify(x => x.GetFilteredList(searchModel), Times.Once);
            storageMock.Verify(x => x.GetFullList(), Times.Never);
        }

        [TestMethod]
        public async Task ReadList_StorageReturnsNull_ShouldReturnNull()
        {
            var storageMock = new Mock<IUserStorage>();

            storageMock
                .Setup(x => x.GetFullList())
                .ReturnsAsync((List<UserModel>?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadList(null);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetFullList(), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.ReadElement(null!));

            storageMock.Verify(x => x.GetElement(It.IsAny<UserSearchModel>()), Times.Never);
        }

        [TestMethod]
        public async Task ReadElement_UserExists_ShouldReturnUser()
        {
            var storageMock = new Mock<IUserStorage>();

            var searchModel = new UserSearchModel
            {
                Email = "test@test.ru"
            };

            var user = CreateValidUser();

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync(user);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNotNull(result);
            Assert.AreEqual(user.Id, result.Id);
            Assert.AreEqual(user.Email, result.Email);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task ReadElement_UserDoesNotExist_ShouldReturnNull()
        {
            var storageMock = new Mock<IUserStorage>();

            var searchModel = new UserSearchModel
            {
                Email = "missing@test.ru"
            };

            storageMock
                .Setup(x => x.GetElement(searchModel))
                .ReturnsAsync((UserModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.ReadElement(searchModel);

            Assert.IsNull(result);

            storageMock.Verify(x => x.GetElement(searchModel), Times.Once);
        }

        [TestMethod]
        public async Task Create_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Create(null!));

            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_UsernameIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Username = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_EmailIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Email = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_EmailHasWrongFormat_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Email = "wrong-email";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_PasswordHashIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.PasswordHash = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_EmailAlreadyExists_ShouldThrowInvalidOperationException()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();
            model.Id = 1;

            var existingUser = CreateValidUser();
            existingUser.Id = 2;
            existingUser.Email = model.Email;

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync(existingUser);

            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await logic.Create(model));

            storageMock.Verify(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)), Times.Once);
            storageMock.Verify(x => x.Insert(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync((UserModel?)null);

            storageMock
                .Setup(x => x.Insert(model))
                .ReturnsAsync(model);

            var logic = CreateLogic(storageMock);

            var result = await logic.Create(model);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)), Times.Once);
            storageMock.Verify(x => x.Insert(model), Times.Once);
        }

        [TestMethod]
        public async Task Create_StorageReturnsNull_ShouldReturnFalse()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync((UserModel?)null);

            storageMock
                .Setup(x => x.Insert(model))
                .ReturnsAsync((UserModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Create(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Insert(model), Times.Once);
        }

        [TestMethod]
        public async Task Update_ModelIsNull_ShouldThrowArgumentNullException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await logic.Update(null!));

            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_UsernameIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Username = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_EmailIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Email = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_EmailHasWrongFormat_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.Email = "wrong-email";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_PasswordHashIsEmpty_ShouldThrowArgumentException()
        {
            var storageMock = new Mock<IUserStorage>();
            var logic = CreateLogic(storageMock);

            var model = CreateValidUser();
            model.PasswordHash = "";

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_EmailBelongsToAnotherUser_ShouldThrowInvalidOperationException()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();
            model.Id = 1;

            var existingUser = CreateValidUser();
            existingUser.Id = 2;
            existingUser.Email = model.Email;

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync(existingUser);

            var logic = CreateLogic(storageMock);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await logic.Update(model));

            storageMock.Verify(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)), Times.Once);
            storageMock.Verify(x => x.Update(It.IsAny<UserModel>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_EmailBelongsToSameUser_ShouldReturnTrue()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();
            model.Id = 1;

            var existingUser = CreateValidUser();
            existingUser.Id = 1;
            existingUser.Email = model.Email;

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync(existingUser);

            storageMock
                .Setup(x => x.Update(model))
                .ReturnsAsync(model);

            var logic = CreateLogic(storageMock);

            var result = await logic.Update(model);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)), Times.Once);
            storageMock.Verify(x => x.Update(model), Times.Once);
        }

        [TestMethod]
        public async Task Update_ValidModel_ShouldReturnTrue()
        {
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync((UserModel?)null);

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
            var storageMock = new Mock<IUserStorage>();

            var model = CreateValidUser();

            storageMock
                .Setup(x => x.GetElement(It.Is<UserSearchModel>(m => m.Email == model.Email)))
                .ReturnsAsync((UserModel?)null);

            storageMock
                .Setup(x => x.Update(model))
                .ReturnsAsync((UserModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Update(model);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Update(model), Times.Once);
        }

        [TestMethod]
        public async Task Delete_UserExists_ShouldReturnTrue()
        {
            var storageMock = new Mock<IUserStorage>();

            var user = CreateValidUser();

            storageMock
                .Setup(x => x.Delete(user.Id))
                .ReturnsAsync(user);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(user.Id);

            Assert.IsTrue(result);

            storageMock.Verify(x => x.Delete(user.Id), Times.Once);
        }

        [TestMethod]
        public async Task Delete_UserDoesNotExist_ShouldReturnFalse()
        {
            var storageMock = new Mock<IUserStorage>();

            storageMock
                .Setup(x => x.Delete(1))
                .ReturnsAsync((UserModel?)null);

            var logic = CreateLogic(storageMock);

            var result = await logic.Delete(1);

            Assert.IsFalse(result);

            storageMock.Verify(x => x.Delete(1), Times.Once);
        }
    }
}