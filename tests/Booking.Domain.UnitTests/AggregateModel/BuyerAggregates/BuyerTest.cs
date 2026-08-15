using BookingService.Domain.AggregateModel.BuyerAggregate;
using BookingService.Domain.Events;
using BookingService.Domain.Exceptions;

namespace Booking.Domain.UnitTests.AggregateModel.BuyerAggregate;

public class BuyerTests
{
    [Fact]
    public void Constructor_WhenValidInput_ShouldCreateBuyer()
    {
        var buyer = new Buyer("user-1", "Test User");

        Assert.Equal("user-1", buyer.IdentityGuid);
        Assert.Equal("Test User", buyer.Name);
        Assert.Empty(buyer.PaymentMethods);
    }

    [Fact]
    public void Constructor_WhenIdentityGuidIsEmpty_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Buyer("", "Test User"));
    }

    [Fact]
    public void Constructor_WhenNameIsEmpty_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Buyer("user-1", ""));
    }

    [Fact]
    public void VerifyOrAddPaymentMethod_WhenPaymentMethodDoesNotExist_ShouldAddPaymentMethod()
    {
        var buyer = new Buyer("user-1", "Test User");

        var paymentMethod = buyer.VerifyOrAddPaymentMethod(
            cardTypeId: 1,
            alias: "Visa",
            cardNumber: "4111111111111111",
            securityNumber: "123",
            cardHolderName: "Test User",
            expiration: DateTime.UtcNow.AddYears(1),
            bookingId: 10
        );

        Assert.NotNull(paymentMethod);
        Assert.Single(buyer.PaymentMethods);
        Assert.Contains(paymentMethod, buyer.PaymentMethods);
    }

    [Fact]
    public void VerifyOrAddPaymentMethod_WhenPaymentMethodAlreadyExists_ShouldReturnExistingPaymentMethod()
    {
        var buyer = new Buyer("user-1", "Test User");
        var expiration = DateTime.UtcNow.AddYears(1);

        var firstPaymentMethod = buyer.VerifyOrAddPaymentMethod(
            cardTypeId: 1,
            alias: "Visa",
            cardNumber: "4111111111111111",
            securityNumber: "123",
            cardHolderName: "Test User",
            expiration: expiration,
            bookingId: 10
        );

        var secondPaymentMethod = buyer.VerifyOrAddPaymentMethod(
            cardTypeId: 1,
            alias: "Visa duplicated",
            cardNumber: "4111111111111111",
            securityNumber: "999",
            cardHolderName: "Another Name",
            expiration: expiration,
            bookingId: 11
        );

        Assert.Same(firstPaymentMethod, secondPaymentMethod);
        Assert.Single(buyer.PaymentMethods);
    }

    [Fact]
    public void VerifyOrAddPaymentMethod_ShouldAddBuyerPaymentMethodVerifiedDomainEvent()
    {
        var buyer = new Buyer("user-1", "Test User");

        buyer.VerifyOrAddPaymentMethod(
            cardTypeId: 1,
            alias: "Visa",
            cardNumber: "4111111111111111",
            securityNumber: "123",
            cardHolderName: "Test User",
            expiration: DateTime.UtcNow.AddYears(1),
            bookingId: 10
        );

        Assert.Contains(
            buyer.DomainEvents,
            domainEvent => domainEvent is BuyerPaymentMethodVerifiedDomainEvent
        );
    }

    [Fact]
    public void VerifyOrAddPaymentMethod_WhenCardTypeIdIsInvalid_ShouldThrowBookingDomainException()
    {
        var buyer = new Buyer("user-1", "Test User");

        Assert.Throws<BookingDomainException>(() =>
            buyer.VerifyOrAddPaymentMethod(
                cardTypeId: 0,
                alias: "Visa",
                cardNumber: "4111111111111111",
                securityNumber: "123",
                cardHolderName: "Test User",
                expiration: DateTime.UtcNow.AddYears(1),
                bookingId: 10
            ));
    }

    [Fact]
    public void VerifyOrAddPaymentMethod_WhenExpirationIsPast_ShouldThrowBookingDomainException()
    {
        var buyer = new Buyer("user-1", "Test User");

        Assert.Throws<BookingDomainException>(() =>
            buyer.VerifyOrAddPaymentMethod(
                cardTypeId: 1,
                alias: "Visa",
                cardNumber: "4111111111111111",
                securityNumber: "123",
                cardHolderName: "Test User",
                expiration: DateTime.UtcNow.AddDays(-1),
                bookingId: 10
            ));
    }
}