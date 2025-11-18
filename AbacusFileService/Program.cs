using AbacusFileService.Extensions;
using AbacusFileService.Interfaces;
using AbacusFileService.Middlewares;
using AbacusFileService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();


builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8080));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", cors =>
    {
        cors.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

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
app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();