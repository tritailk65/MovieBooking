namespace Seat.API.Domain.Interfaces;

public interface ISeatRepository
{
    Task InitializeSeatsAsync(int showtimeId, ShowtimeSeat seats);
    Task<ShowtimeSeat> GetShowtimeSeatsAsync(int showtimeId);
    Task<Entities.Seat> GetSeatHashAsync(int showtimeId, string seatId);
    Task SetSeatHashAysnc(int showtimeId, string seatId, string value);
    Task SetSeatReservationHashAsync(SeatReservation seatReservation);
    Task<SeatReservation> GetSeatReservationHashAsync(int showtimeId, string userId);
    Task RemoveSeatFromReservationHashAsync(int showtimeId, string userId, string seatId);
    Task<bool> ReleaseSeatReservationHashAsync(int showtimeId, string userId);
}
