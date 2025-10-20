namespace EVCharging.PaymentService.Services;

public class PricingService : IPricingService
{
    private const decimal BaseFeeVND = 5000; // 5,000 VND base fee
    private const decimal PricePerKwhVND = 3500; // 3,500 VND per kWh

    public decimal CalculateChargingCost(decimal energyConsumed, int durationMinutes)
    {
        // Hybrid pricing: base fee + per kWh
        var energyCost = energyConsumed * PricePerKwhVND;
        var totalCost = BaseFeeVND + energyCost;
        
        return Math.Round(totalCost, 0);
    }

    public decimal ApplyDiscount(decimal amount, decimal discountPercentage)
    {
        var discountAmount = amount * (discountPercentage / 100);
        return Math.Round(amount - discountAmount, 0);
    }
}