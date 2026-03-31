using Api.Services;
using Core;
using SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddSingleton<SavedConnectionStore>();
builder.Services.AddScoped<IQueryExecutor, SqlServerQueryExecutor>();
builder.Services.AddScoped<IExplorerService, SqlServerExplorerService>();

var app = builder.Build();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
