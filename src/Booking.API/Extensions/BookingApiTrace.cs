using BookingService.Domain.AggregateModel.BookingAggregates;

namespace BookingService.API.Extensions;

// Helper giúp tái sử dụng log.Trace
internal static partial class BookingApiTrace
{
    [LoggerMessage(EventId = 1, EventName = "OrderStatusUpdated", Level = LogLevel.Trace, Message = "Booking with Id: {BookingId} has been successfully updated to status {Status}")]
    public static partial void LogBookingStatusUpdated(ILogger logger, int bookingId, BookingStatus status);

    [LoggerMessage(EventId = 2, EventName = "PaymentMethodUpdated", Level = LogLevel.Trace, Message = "Booking with Id: {BookingId} has been successfully updated with a payment method {PaymentMethod} ({Id})")]
    public static partial void LogBookingPaymentMethodUpdated(ILogger logger, int bookingId, string paymentMethod, int id);

    [LoggerMessage(EventId = 3, EventName = "BuyerAndPaymentValidatedOrUpdated", Level = LogLevel.Trace, Message = "Buyer {BuyerId} and related payment method were validated or updated for Booking Id: {BookingId}.")]
    public static partial void LogBookingBuyerAndPaymentValidatedOrUpdated(ILogger logger, int buyerId, int bookingId);
}
