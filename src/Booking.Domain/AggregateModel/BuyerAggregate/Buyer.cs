using System.ComponentModel.DataAnnotations;
using BookingService.Domain.Events;
using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BuyerAggregate;

public class Buyer : Entity, IAggregateRoot
{
    // ID của User lấy từ Identity Service
    [Required]
    public string IdentityGuid { get; private set; }

    [Required]
    public string Name { get; private set; }

    // Backing field cho danh sách thẻ thanh toán
    private readonly List<PaymentMethod> _paymentMethods;
    public IReadOnlyCollection<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    protected Buyer()
    {
        _paymentMethods = new List<PaymentMethod>();
    }

    public Buyer(string identityGuid, string name) : this()
    {
        IdentityGuid = !string.IsNullOrWhiteSpace(identityGuid) ? identityGuid : throw new ArgumentNullException(nameof(identityGuid));
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
    }

    // Hàm thêm hoặc xác thực phương thức thanh toán
    public PaymentMethod VerifyOrAddPaymentMethod(int cardTypeId, string alias, string cardNumber, string securityNumber, string cardHolderName, DateTime expiration, int bookingId)
    {
        var existingPayment = _paymentMethods.SingleOrDefault(p => p.IsEqualTo(cardTypeId, cardNumber, expiration));

        if (existingPayment != null)
        {
            // Nếu thẻ đã tồn tại, kích hoạt Event báo hiệu đã xác thực thành công thẻ cũ
            AddDomainEvent(new BuyerPaymentMethodVerifiedDomainEvent(this, existingPayment, bookingId));
            return existingPayment;
        }

        // Nếu là thẻ mới, tạo mới và add vào danh sách
        var payment = new PaymentMethod(cardTypeId, alias, cardNumber, securityNumber, cardHolderName, expiration);
        _paymentMethods.Add(payment);

        // Kích hoạt Event báo hiệu vừa thêm thẻ mới thành công
        AddDomainEvent(new BuyerPaymentMethodVerifiedDomainEvent(this, payment, bookingId));

        return payment;
    }
}
