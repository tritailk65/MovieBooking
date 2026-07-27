using System.ComponentModel.DataAnnotations;
using BookingService.Domain.Exceptions;
using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BuyerAggregate;

public class PaymentMethod : Entity
{
    public string Alias { get; private set; }
    public string CardNumber { get; private set; }
    [Required]
    public string SecurityNumber { get; private set; }
    public string CardHolderName { get; private set; }
    public DateTime Expiration { get; private set; }

    private int _cardTypeId;
    public CardType CardType { get; private set; }

    protected PaymentMethod() { }

    internal PaymentMethod(int cardTypeId, string alias, string cardNumber, string securityNumber, string cardHolderName, DateTime expiration)
    {
        if (cardTypeId <= 0)
        {
            throw new BookingDomainException("Card type is required.");
        }

        CardNumber = !string.IsNullOrWhiteSpace(cardNumber) ? cardNumber : throw new BookingDomainException(nameof(cardNumber));
        SecurityNumber = !string.IsNullOrWhiteSpace(securityNumber) ? securityNumber : throw new BookingDomainException(nameof(securityNumber));
        CardHolderName = !string.IsNullOrWhiteSpace(cardHolderName) ? cardHolderName : throw new BookingDomainException(nameof(cardHolderName));

        if (expiration < DateTime.UtcNow)
        {
            throw new BookingDomainException("Thẻ thanh toán đã hết hạn.");
        }

        Alias = alias;
        Expiration = expiration;
        _cardTypeId = cardTypeId;
    }

    // So sánh thẻ được add có bị trùng hay không
    public bool IsEqualTo(int cardTypeId, string cardNumber, DateTime expiration)
    {
        return _cardTypeId == cardTypeId
            && CardNumber == cardNumber
            && Expiration == expiration;
    }
}
