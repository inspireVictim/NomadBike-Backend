using Bikes.API.Data;
using Bikes.API.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL
builder.Services.AddDbContext<BikesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// Configure MQTT service as Singleton and HostedService
builder.Services.AddSingleton<MqttBikeControllerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttBikeControllerService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Auto-migrate for local dev
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BikesDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

// ---- minimal API endpoints ----

app.MapGet("/api/bikes", async (BikesDbContext db) =>
    await db.Bikes.ToListAsync());

app.MapGet("/api/bikes/{id:guid}", async (Guid id, BikesDbContext db) =>
    await db.Bikes.FindAsync(id) is { } bike ? Results.Ok(bike) : Results.NotFound());

app.MapPost("/api/bikes", async (Bikes.API.Models.Bike bike, BikesDbContext db) =>
{
    if (bike.Id == Guid.Empty) bike.Id = Guid.NewGuid();
    bike.LastUpdatedAt = DateTime.UtcNow;
    db.Bikes.Add(bike);
    await db.SaveChangesAsync();
    return Results.Created($"/api/bikes/{bike.Id}", bike);
});

app.MapPost("/api/bikes/{id:guid}/location", async (Guid id, double lat, double lon, BikesDbContext db, IConnectionMultiplexer redis) =>
{
    var bike = await db.Bikes.FindAsync(id);
    if (bike is null) return Results.NotFound();

    bike.Latitude = lat;
    bike.Longitude = lon;
    bike.LastUpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var dbRedis = redis.GetDatabase();
    await dbRedis.GeoAddAsync("bikes:locations", lon, lat, id.ToString());

    return Results.Ok(new { bike.Id, bike.Latitude, bike.Longitude, bike.LastUpdatedAt });
});

app.MapPost("/api/bikes/{id:guid}/lock", async (Guid id, bool isLocked, BikesDbContext db, MqttBikeControllerService mqttClient) =>
{
    var bike = await db.Bikes.FindAsync(id);
    if (bike is null) return Results.NotFound();

    // Send physical command to IoT device via MQTT
    await mqttClient.SendLockCommandAsync(bike.SerialNumber, isLocked);

    // Update logical status in DB
    bike.Status = isLocked ? Bikes.API.Models.BikeStatus.Available : Bikes.API.Models.BikeStatus.InUse;
    await db.SaveChangesAsync();

    return Results.Ok(new { bike.Id, bike.Status });
});

app.Run();
