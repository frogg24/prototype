using BusinessLogic;
using Database.Implements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<UserStorage>();
builder.Services.AddScoped<ReadStorage>();
builder.Services.AddScoped<ProjectStorage>();
builder.Services.AddScoped<AssemblyStorage>();
builder.Services.AddScoped<UserLogic>();
builder.Services.AddScoped<ReadLogic>();
builder.Services.AddScoped<ProjectLogic>();
builder.Services.AddScoped<AssemblyLogic>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
