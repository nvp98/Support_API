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

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Program.cs (trước dòng var builder = WebApplication.CreateBuilder(args);)
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

var app = builder.Build();

// Cho phép truy cập ảnh trong wwwroot/uploads qua URL /uploads/*
app.UseStaticFiles();

//app.UseCors("AllowReactApp"); // Áp dụng CORS
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Đặt route Swagger UI tại /api
    options.RoutePrefix = "api";

    // Chỉ định endpoint JSON (vẫn nằm ở /swagger/v1/swagger.json)
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
});

// Cho phép đọc React build từ wwwroot
app.UseStaticFiles();

// Nếu không trúng API nào → trả về index.html (React handle routing)
app.MapFallbackToFile("index.html");

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//};

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
