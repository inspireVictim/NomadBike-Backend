namespace Payments.API.Models;

public class CustomerProfile
{
    public Guid UserId { get; set; } // Внешний ключ из Identity.API
    public string StripeCustomerId { get; set; } = string.Empty;
    public bool HasValidPaymentMethod { get; set; }
}
