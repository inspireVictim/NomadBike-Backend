using Microsoft.EntityFrameworkCore;
using Payments.API.Data;
using Payments.API.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

// 1. Привязка карты (SetupIntent) - безопасно передает данные карты в Stripe из мобилки
app.MapPost("/api/payments/setup-intent", async (Guid userId, PaymentsDbContext db) =>
{
    var profile = await db.CustomerProfiles.FindAsync(userId);
    string customerId;

    if (profile is null)
    {
        var options = new CustomerCreateOptions { Metadata = new Dictionary<string, string> { { "UserId", userId.ToString() } } };
        var service = new CustomerService();
        var customer = await service.CreateAsync(options);
        
        customerId = customer.Id;
        profile = new CustomerProfile { UserId = userId, StripeCustomerId = customerId, HasValidPaymentMethod = false };
        db.CustomerProfiles.Add(profile);
        await db.SaveChangesAsync();
    }
    else
    {
        customerId = profile.StripeCustomerId;
    }

    var setupOptions = new SetupIntentCreateOptions { Customer = customerId, PaymentMethodTypes = new List<string> { "card" } };
    var setupService = new SetupIntentService();
    var intent = await setupService.CreateAsync(setupOptions);

    return Results.Ok(new { ClientSecret = intent.ClientSecret });
});

// 2. Холдирование депозита 50$ (Auth without Capture)
app.MapPost("/api/payments/hold", async (Guid userId, Guid tripId, PaymentsDbContext db) =>
{
    var profile = await db.CustomerProfiles.FindAsync(userId);
    if (profile is null || !profile.HasValidPaymentMethod) 
        return Results.BadRequest("Нет привязанного способа оплаты.");

    var options = new PaymentIntentCreateOptions
    {
        Amount = 5000, // $50.00 в центах
        Currency = "usd",
        Customer = profile.StripeCustomerId,
        CaptureMethod = "manual", // Только авторизация
        Confirm = true, 
        OffSession = true 
    };

    var service = new PaymentIntentService();
    try
    {
        var intent = await service.CreateAsync(options);
        var tx = new PaymentTransaction
        {
            Id = Guid.NewGuid(), UserId = userId, TripId = tripId, Amount = 50.00m,
            StripePaymentIntentId = intent.Id, Status = TransactionStatus.Pending, CreatedAt = DateTime.UtcNow
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        return Results.Ok(new { Success = true, TransactionId = tx.Id });
    }
    catch (StripeException e)
    {
        return Results.BadRequest(new { Error = e.StripeError.Message, Code = e.StripeError.Code });
    }
});

// 3. Списание стоимости после поездки
app.MapPost("/api/payments/capture", async (Guid tripId, decimal finalCost, PaymentsDbContext db) =>
{
    var tx = await db.Transactions.FirstOrDefaultAsync(t => t.TripId == tripId && t.Status == TransactionStatus.Pending);
    if (tx is null) return Results.NotFound("Транзакция холдирования не найдена.");

    long amountToCapture = (long)(finalCost * 100); 
    var service = new PaymentIntentService();
    try
    {
        var options = new PaymentIntentCaptureOptions { AmountToCapture = amountToCapture };
        await service.CaptureAsync(tx.StripePaymentIntentId, options);
        
        tx.Status = TransactionStatus.Captured;
        tx.Amount = finalCost;
        await db.SaveChangesAsync();

        return Results.Ok(new { Success = true });
    }
    catch (StripeException e)
    {
        tx.Status = TransactionStatus.Failed;
        await db.SaveChangesAsync();
        return Results.BadRequest(new { Error = e.StripeError.Message });
    }
});

app.Run();
