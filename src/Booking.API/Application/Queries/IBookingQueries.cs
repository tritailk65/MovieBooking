using BookingService.API.Application.Queries.ViewModels;

namespace BookingService.API.Application.Queries;

public interface IBookingQueries
{
    Task<BookingVM> GetBookingAsync(int id);

    Task<int?> GetBookingIdByReservationAsync(Guid reservationId);

    Task<IEnumerable<BookingVM>> GetBookingFromUserAsync(string userId);

    Task<IEnumerable<CardTypeVM>> GetCardTypesAsync();
}
