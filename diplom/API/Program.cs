using API.Logging;
using BusinessLogic;
using Database.Implements;
using DataModels.Interfaces;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<UserStorage>();
    builder.Services.AddScoped<ReadStorage>();
    builder.Services.AddScoped<ProjectStorage>();
    builder.Services.AddScoped<AssemblyStorage>();

    builder.Services.AddScoped<UserLogic>();
    builder.Services.AddScoped<ReadLogic>();
    builder.Services.AddScoped<ProjectLogic>();
    builder.Services.AddScoped<AssemblyLogic>();

    builder.Services.AddScoped<AlgorithmOLC>();

    builder.Services.AddScoped<IAdminAuditLogger, AdminAuditLogger>();

    builder.Services.AddScoped<IUserStorage, UserStorage>();
    builder.Services.AddScoped<IProjectStorage, ProjectStorage>();
    builder.Services.AddScoped<IReadStorage, ReadStorage>();
    builder.Services.AddScoped<IAssemblyStorage, AssemblyStorage>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}