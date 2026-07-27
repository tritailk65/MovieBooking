using BookingService.Domain.AggregateModel.BuyerAggregate;
using BookingService.Domain.Events;
using BookingService.Domain.Exceptions;
using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BookingAggregates;

public class Booking : Entity, IAggregateRoot
{
    public string UserId { get; private set; }
    public int ShowtimeId { get; private set; }
    public int HallId { get; private set; }
    public DateTime BookingAt { get; private set; }
    public Buyer Buyer {get; private set;}
    private int _bookingStatusId;
    public BookingStatus BookingStatus { get; private set; }

    private bool _isDraft; 

    // Backing field de ef core map vao database
    // bao ve danh sach khoi bi add/remove bua bai
    private readonly List<BookingItem> _bookingItems;
    public IReadOnlyCollection<BookingItem> BookingItems => _bookingItems.AsReadOnly();

    public int? PaymentId {get; private set;}

    public Guid ReservationId {get; private set;}

    public static Booking NewDraft()
    {
        var booking = new Booking{ _isDraft = true};
        return booking;
    }

    protected Booking()
    {
        _bookingItems = new List<BookingItem>();
        _isDraft = false;
    } 

    public Booking(string userId, string userName, int showtimeId, Guid reservationId) : this()
    {

        if (string.IsNullOrEmpty(userId))
        {
            throw new BookingDomainException("User id is required.");
        }

        if (string.IsNullOrEmpty(userName))
        {
            throw new BookingDomainException("User id is required.");
        }


        if (showtimeId <= 0)
        {
            throw new BookingDomainException("Showtime id is required.");
        }

        if (reservationId == default)
        {
            throw new BookingDomainException("Reservation id is required");
        }


        UserId = userId;
        ShowtimeId = showtimeId;
        ReservationId = reservationId;
        BookingAt = DateTime.UtcNow;

        _bookingStatusId = BookingStatus.Submitted.Id;
        
        // Add Domain event to save Buyer entity
        AddBookingStartedDomainEvent(userId, userName);

        AddDomainEvent(new BookingCreatedDomainEvent(this));
    }

    public void AddBookingItem(int showtimeId, string seatId, decimal basePrice)
    {
        if (showtimeId <= 0)
        {
            throw new BookingDomainException("Showtime id is required.");
        }

        if (string.IsNullOrWhiteSpace(seatId))
        {
            throw new BookingDomainException("Seat id is required.");
        }

        var existingSeat = _bookingItems.SingleOrDefault(i => i.SeatId == seatId);

        if (existingSeat != null)
        {
            throw new BookingDomainException($"Seat {seatId} is already booking");
        }

        var item = new BookingItem(showtimeId, seatId, basePrice);

        _bookingItems.Add(item);
    }

    // Tính tổng tiền đơn hàng
    public decimal GetTotal()
    {
        return _bookingItems.Sum(item => item.BasePrice);
    }

    // Cập nhật trạng thái khi Seat Service khóa ghế thành công
    public void SetSeatConfirmedStatus()
    {
        if (_bookingStatusId != BookingStatus.Submitted.Id && _bookingStatusId != BookingStatus.AwaitingSeatValidation.Id)
        {
            StatusChangeException(BookingStatus.SeatConfirmed);
        }

        _bookingStatusId = BookingStatus.SeatConfirmed.Id;

        AddDomainEvent(new BookingSeatConfirmedDomainEvent(this));
    }

    private void AddBookingStartedDomainEvent(string userId, string userName)
    {
        var bookingStartedDomainEvent = new BookingStartedDomainEvent(this, userName, userId);

        AddDomainEvent(bookingStartedDomainEvent);
    }

    public void SetAwaitingSeatValidationStatus()
    {
        if (_bookingStatusId != BookingStatus.Submitted.Id)
        {
            throw new BookingDomainException("Không thể chuyển sang trạng thái chờ xác thực thanh toan khi don hang không ở trạng thái submitted.");
        }

        _bookingStatusId = BookingStatus.AwaitingSeatValidation.Id;
        AddDomainEvent(new BookingAwaitingPaymentDomainEvent(this));
    }

    // Cập nhật trạng thái khi Payment Service báo thanh toán thành công
    public void SetPaidStatus()
    {
        if (_bookingStatusId != BookingStatus.SeatConfirmed.Id &&
            _bookingStatusId != BookingStatus.AwaitingSeatValidation.Id)
        {
            StatusChangeException(BookingStatus.Paid);
        }

        _bookingStatusId = BookingStatus.Paid.Id;
        AddDomainEvent(new BookingPaidDomainEvent(this));
    }

    // Hủy đơn hàng
    public void SetCancelledStatus()
    {
        if (_bookingStatusId == BookingStatus.Paid.Id)
        {
            throw new BookingDomainException("Không thể hủy đơn hàng đã thanh toán thành công.");
        }

        _bookingStatusId = BookingStatus.Cancelled.Id;
        AddDomainEvent(new BookingCancelledDomainEvent(this));
    }

    private void StatusChangeException(BookingStatus statusToChange)
    {
        throw new BookingDomainException($"Không thể đổi trạng thái đơn đặt vé sang {statusToChange.Name}.");
    }
}
