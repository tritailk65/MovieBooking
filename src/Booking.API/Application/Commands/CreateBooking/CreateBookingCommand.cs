using System.Runtime.Serialization;
using BookingService.API.Application.Models;
using MediatR;

namespace BookingService.API.Application.Commands.CreateBooking;

public record CreateBookingCommand : IRequest<bool>
{
    [DataMember]
    public string UserId { get; private set; }

    [DataMember]
    public string UserName { get; private set; }

    [DataMember]
    public string City { get; private set; }

    [DataMember]
    public string Street { get; private set; }

    [DataMember]
    public string State { get; private set; }

    [DataMember]
    public string Country { get; private set; }

    [DataMember]
    public string ZipCode { get; private set; }

    [DataMember]
    public string CardNumber { get; private set; }

    [DataMember]
    public string CardHolderName { get; private set; }

    [DataMember]
    public DateTime CardExpiration { get; private set; }

    [DataMember]
    public string CardSecurityNumber { get; private set; }

    [DataMember]
    public int CardTypeId { get; private set; }

    [DataMember]
    public int ShowtimeId {get; private set; }

    [DataMember]
    public int HallId {get; private set;}

    [DataMember]
    public Guid ReservationId {get; private set;}

    [DataMember]
    private readonly List<SeatItem> _seatItem;

    [DataMember]
    public IEnumerable<SeatItem> BookingItem => _seatItem;

    public CreateBookingCommand()
    {
        _seatItem = new List<SeatItem>();
    }


    public CreateBookingCommand(IEnumerable<SeatItem> seatItems, string userId, string userName, int showtimeId, Guid reservationId)
    {
        _seatItem = seatItems.ToList();
        UserId = userId;
        UserName = userName;
        ShowtimeId = showtimeId;
        ReservationId = reservationId;
    }
}