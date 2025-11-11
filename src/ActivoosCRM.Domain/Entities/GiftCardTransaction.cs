using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

public class GiftCardTransaction : BaseEntity
{
    private GiftCardTransaction() { }

    public Guid GiftCardId { get; private set; }
    public virtual GiftCard GiftCard { get; private set; } = null!;

    public Guid? BookingId { get; private set; }
    public virtual Booking? Booking { get; private set; }

    public decimal AmountUsed { get; private set; }
    public decimal BalanceAfter { get; private set; }

    public static GiftCardTransaction Create(
        Guid giftCardId,
        Guid? bookingId,
        decimal amountUsed,
        decimal balanceAfter)
    {
        return new GiftCardTransaction
        {
            Id = Guid.NewGuid(),
            GiftCardId = giftCardId,
            BookingId = bookingId,
            AmountUsed = amountUsed,
            BalanceAfter = balanceAfter
        };
    }
}