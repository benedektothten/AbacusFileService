using AbacusFileService.Extensions;
using AbacusFileService.Interfaces;
using AbacusFileService.Middlewares;
using AbacusFileService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();


builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8080));

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure and add Azure Services
builder.Services.ConfigureAzureServices(builder.Configuration);
builder.Services.AddAzureServices(builder.Configuration);

builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();