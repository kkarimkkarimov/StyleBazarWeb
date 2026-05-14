namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// Payment options. MVP supports CashOnDelivery only. Card / Wallet land
/// when we integrate a gateway in a later phase.
/// </summary>
public enum PaymentMethod
{
    CashOnDelivery = 0,
    Card = 1,
    Wallet = 2
}
