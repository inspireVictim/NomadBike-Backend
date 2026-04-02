namespace Payments.API.Models;

public enum TransactionStatus { Pending = 0, Captured = 1, Failed = 2, Refunded = 3 }

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TripId { get; set; }
    
    public decimal Amount { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
