using BusinessLogic;
using DataModels.AssemblyModels;
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
    public class AssemblyLoadTests
    {
        [TestMethod]
        public async Task RunAssembly_LoadTest()
        {
            var userStorage = new UserStorage(NullLogger<UserStorage>.Instance);
            var projectStorage = new ProjectStorage(NullLogger<ProjectStorage>.Instance);
            var readStorage = new ReadStorage(NullLogger<ReadStorage>.Instance);
            var assemblyStorage = new AssemblyStorage(NullLogger<AssemblyStorage>.Instance);

            var userLogic = new UserLogic(userStorage, NullLogger<UserLogic>.Instance);
            var projectLogic = new ProjectLogic(projectStorage, NullLogger<ProjectLogic>.Instance);
            var readLogic = new ReadLogic(readStorage, NullLogger<ReadLogic>.Instance);

            var algorithmOLC = new AlgorithmOLC(
                readStorage,
                NullLogger<AlgorithmOLC>.Instance
            );

            var assemblyLogic = new AssemblyLogic(
                assemblyStorage,
                algorithmOLC,
                NullLogger<AssemblyLogic>.Instance
            );

            UserModel? createdUser = null;
            var createdProjects = new List<ProjectModel>();

            try
            {
                var userEmail = $"assembly_load_test_{Guid.NewGuid():N}@test.ru";

                var userModel = new UserModel
                {
                    Username = $"assembly_load_test_{Guid.NewGuid():N}",
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

                var filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Ab1");

                Assert.IsTrue(
                    Directory.Exists(filesDirectory),
                    $"Папка с тестовыми .ab1 файлами не найдена: {filesDirectory}"
                );

                var sourceFiles = Directory.GetFiles(filesDirectory, "*.ab1");

                Assert.IsTrue(sourceFiles.Length >= 4, "Для теста сборки нужно минимум 4 .ab1 файла");

                var tests = new[] { 5, 10, 50, 100 };

                foreach (var projectsCount in tests)
                {
                    var currentProjects = new List<ProjectModel>();

                    for (int projectIndex = 0; projectIndex < projectsCount; projectIndex++)
                    {
                        var projectTitle = $"Assembly load test project {projectsCount}_{projectIndex}_{Guid.NewGuid():N}";

                        var projectModel = new ProjectModel
                        {
                            UserId = createdUser.Id,
                            Title = projectTitle,
                            CreatedAt = DateTime.UtcNow
                        };

                        var projectCreated = await projectLogic.Create(projectModel);

                        Assert.IsTrue(projectCreated, $"Не удалось создать тестовый проект {projectIndex}");

                        var createdProject = await projectLogic.ReadElement(new ProjectSearchModel
                        {
                            Title = projectTitle
                        });

                        Assert.IsNotNull(createdProject, $"Тестовый проект {projectIndex} не найден после создания");

                        currentProjects.Add(createdProject);
                        createdProjects.Add(createdProject);

                        var uploadFiles = new List<UploadReadFileModel>();

                        for (int fileIndex = 0; fileIndex < 4; fileIndex++)
                        {
                            var filePath = sourceFiles[fileIndex];

                            uploadFiles.Add(new UploadReadFileModel
                            {
                                FileName = $"assembly_load_{projectsCount}_{projectIndex}_{fileIndex}_{Path.GetFileName(filePath)}",
                                Content = await File.ReadAllBytesAsync(filePath)
                            });
                        }

                        var uploadedReads = await readLogic.UploadReadsAsync(createdProject.Id, uploadFiles);

                        Assert.AreEqual(4, uploadedReads.Count, $"В проект {createdProject.Id} загружено не 4 рида");
                    }

                    var stopwatch = Stopwatch.StartNew();

                    foreach (var project in currentProjects)
                    {
                        var assembly = await assemblyLogic.MakeOLC(project.Id);

                        Assert.IsNotNull(assembly);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(assembly.ConsensusSequence));
                        Assert.IsTrue(assembly.ConsensusLength > 0);
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"Количество проектов: {projectsCount}; Ридов в каждом проекте: 4; Всего ридов: {projectsCount * 4}; Время сборки: {stopwatch.ElapsedMilliseconds} мс");
                }
            }
            finally
            {
                foreach (var project in createdProjects)
                {
                    var assemblies = await assemblyLogic.GetProjectAssemblysAsync(project.Id);

                    foreach (var assembly in assemblies)
                    {
                        await assemblyLogic.Delete(assembly.Id);
                    }

                    var reads = await readLogic.GetProjectReadsAsync(project.Id);

                    foreach (var read in reads)
                    {
                        await readLogic.Delete(read.Id);
                    }

                    await projectLogic.Delete(project.Id);
                }

                if (createdUser != null)
                {
                    await userLogic.Delete(createdUser.Id);
                }
            }
        }
    }
}