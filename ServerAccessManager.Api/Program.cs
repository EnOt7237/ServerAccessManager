using Microsoft.EntityFrameworkCore;
using ServerAccessManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Регистрация сервисов (DI Container) ---

builder.Services.AddControllers(); // Добавляем поддержку контроллеров (вместо MapGet)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger для тестирования запросов

// Регистрация подключения к PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- 2. Настройка конвейера (Middleware) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Важно для будущего: сначала аутентификация, потом авторизация
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Включаем поиск контроллеров

app.Run();