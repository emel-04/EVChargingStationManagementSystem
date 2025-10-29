namespace EVCharging.PaymentService.Services;

public interface IPricingService
{
    decimal CalculateChargingCost(decimal energyConsumed, int durationMinutes);
    decimal ApplyDiscount(decimal amount, decimal discountPercentage);
}