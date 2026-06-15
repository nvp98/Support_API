using System.Net;
using Microsoft.EntityFrameworkCore;
using support.server.Controllers;
using support.server.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()  // Cho phép tất cả các origin
               .AllowAnyMethod()  // Cho phép tất cả các method (GET, POST, PUT, DELETE...)
               .AllowAnyHeader(); // Cho phép tất cả các header
    });
    //options.AddPolicy("AllowReact",
    //    policy =>
    //    {
    //        policy.WithOrigins("http://localhost:5173") // Cho phép React truy cập
    //              .AllowAnyHeader()
    //              .AllowAnyMethod();
    //    });
});


// Đăng ký DbContext với Dependency Injection (DI)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

builder.Services.AddDbContext<EPortalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbEPORTALConnectionString")));

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Program.cs (trước dòng var builder = WebApplication.CreateBuilder(args);)
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

var app = builder.Build();

// Global exception handler - phải đặt đầu tiên để catch tất cả exception
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception at {Path}", context.Request.Path);
            await context.Response.WriteAsJsonAsync(new { status = 500, message = "Lỗi máy chủ nội bộ. Vui lòng thử lại sau." });
        }
    });
});

app.UseCors("AllowAllOrigins");

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Nếu không trúng API nào → trả về index.html (React handle routing)
app.MapFallbackToFile("index.html");

app.Run();
