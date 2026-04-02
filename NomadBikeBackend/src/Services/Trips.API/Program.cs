using Microsoft.EntityFrameworkCore;
using Trips.API.Data;
using Trips.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP Client для связи с микросервисом Bikes
builder.Services.AddHttpClient("BikesApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BikesApiUrl"] ?? "http://localhost:5000");
});

builder.Services.AddDbContext<TripsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Автоматическая накатка миграций для локальной разработки
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TripsDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

// ---- Бизнес-логика поездок ----
const decimal PricePerMinute = 2.50m; // Базовая цена в условных единицах (RUB/USD/KZT)

app.MapPost("/api/trips/start", async (Guid userId, Guid bikeId, TripsDbContext db, IHttpClientFactory httpClientFactory) =>
{
    // Защита от дублей. Пользователь может иметь только 1 активную поездку.
    var hasActiveTrip = await db.Trips.AnyAsync(t => t.UserId == userId && t.Status == TripStatus.Active);
    if (hasActiveTrip) return Results.BadRequest("User already has an active trip.");

    // TODO: Verify user balance / hold 50$ via Payments.API

    // 1. Команда в Bikes.API на разблокировку (Lock=false)
    var client = httpClientFactory.CreateClient("BikesApi");
    var unlockResponse = await client.PostAsync($"/api/bikes/{bikeId}/lock?isLocked=false", null);
    
    if (!unlockResponse.IsSuccessStatusCode)
        return Results.BadRequest("Bike is not available, offline, or already in use.");

    // 2. Создание поездки
    var trip = new Trip
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        BikeId = bikeId,
        StartTime = DateTime.UtcNow,
        Status = TripStatus.Active
    };

    db.Trips.Add(trip);
    await db.SaveChangesAsync();

    return Results.Created($"/api/trips/{trip.Id}", trip);
});

app.MapPost("/api/trips/{id:guid}/end", async (Guid id, TripsDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var trip = await db.Trips.FindAsync(id);
    if (trip is null || trip.Status != TripStatus.Active) 
        return Results.NotFound("Active trip not found.");

    // 1. Команда в Bikes.API на блокировку замка
    var client = httpClientFactory.CreateClient("BikesApi");
    var lockResponse = await client.PostAsync($"/api/bikes/{trip.BikeId}/lock?isLocked=true", null);
    
    if (!lockResponse.IsSuccessStatusCode)
    {
        // Не удалось заблокировать. В production здесь будет Dead Letter Queue/Retry механизм.
        // Для MVP будем просто завершать поездку или возвращать ошибку (пусть юзер пробует снова закрыть).
        return Results.StatusCode(500); 
    }

    // 2. Расчет времени и стоимости
    trip.EndTime = DateTime.UtcNow;
    trip.Status = TripStatus.Completed;
    
    var duration = (trip.EndTime.Value - trip.StartTime).TotalMinutes;
    // Округляем до верхней минуты 
    trip.FinalCost = (decimal)Math.Ceiling(duration) * PricePerMinute;

    await db.SaveChangesAsync();

    // TODO: Charge user via Payments.API

    return Results.Ok(new 
    { 
        trip.Id, 
        DurationMinutes = Math.Ceiling(duration), 
        Cost = trip.FinalCost 
    });
});

app.Run();
