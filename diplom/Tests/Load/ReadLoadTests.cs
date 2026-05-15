using BusinessLogic;
using DataModels.ProjectModels;
using DataModels.ReadModels;
using DataModels.UserModels;
using Database.Implements;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Tests.Load
{
    [TestClass]
    public class ReadLoadTests
    {
        [TestMethod]
        public async Task UploadReads_LoadTest()
        {
            var userStorage = new UserStorage(NullLogger<UserStorage>.Instance);
            var projectStorage = new ProjectStorage(NullLogger<ProjectStorage>.Instance);
            var readStorage = new ReadStorage(NullLogger<ReadStorage>.Instance);

            var userLogic = new UserLogic(userStorage, NullLogger<UserLogic>.Instance);
            var projectLogic = new ProjectLogic(projectStorage, NullLogger<ProjectLogic>.Instance);
            var readLogic = new ReadLogic(readStorage, NullLogger<ReadLogic>.Instance);

            UserModel? createdUser = null;
            ProjectModel? createdProject = null;

            try
            {
                var userEmail = $"load_test_{Guid.NewGuid():N}@test.ru";
                var username = $"load_test_{Guid.NewGuid():N}";

                var userModel = new UserModel
                {
                    Username = username,
                    Email = userEmail,
                    PasswordHash = "hash",
                    CreatedAt = DateTime.UtcNow
                };

                var userCreated = await userLogic.Create(userModel);

                Assert.IsTrue(userCreated, "Не удалось создать тестового пользователя");

                createdUser = await userLogic.ReadElement(new UserSearchModel
                {
                    Email = userEmail
                });

                Assert.IsNotNull(createdUser, "Тестовый пользователь не найден после создания");

                var projectTitle = $"Load test project {Guid.NewGuid():N}";

                var projectModel = new ProjectModel
                {
                    UserId = createdUser.Id,
                    Title = projectTitle,
                    CreatedAt = DateTime.UtcNow
                };

                var projectCreated = await projectLogic.Create(projectModel);

                Assert.IsTrue(projectCreated, "Не удалось создать тестовый проект");

                createdProject = await projectLogic.ReadElement(new ProjectSearchModel
                {
                    Title = projectTitle
                });

                Assert.IsNotNull(createdProject, "Тестовый проект не найден после создания");

                var filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Ab1");

                var sourceFiles = Directory.GetFiles(filesDirectory, "*.ab1");

                Assert.IsTrue(sourceFiles.Length > 0, "В папке TestData/Ab1 нет .ab1 файлов");

                var tests = new[] { 10, 25, 50, 100 };

                foreach (var filesCount in tests)
                {
                    var uploadFiles = new List<UploadReadFileModel>();

                    for (int i = 0; i < filesCount; i++)
                    {
                        var filePath = sourceFiles[i % sourceFiles.Length];

                        uploadFiles.Add(new UploadReadFileModel
                        {
                            FileName = $"load_{filesCount}_{i}_{Path.GetFileName(filePath)}",
                            Content = await File.ReadAllBytesAsync(filePath)
                        });
                    }

                    var stopwatch = Stopwatch.StartNew();

                    var result = await readLogic.UploadReadsAsync(createdProject.Id, uploadFiles);

                    stopwatch.Stop();

                    Console.WriteLine($"Количество файлов: {filesCount}; Загружено: {result.Count}; Время: {stopwatch.ElapsedMilliseconds} мс");

                    Assert.AreEqual(filesCount, result.Count, $"Количество загруженных файлов не совпадает для теста {filesCount}");
                }
            }
            finally
            {
                if (createdProject != null)
                {
                    var reads = await readLogic.GetProjectReadsAsync(createdProject.Id);

                    foreach (var read in reads)
                    {
                        await readLogic.Delete(read.Id);
                    }

                    await projectLogic.Delete(createdProject.Id);
                }

                if (createdUser != null)
                {
                    await userLogic.Delete(createdUser.Id);
                }
            }
        }
    }
}