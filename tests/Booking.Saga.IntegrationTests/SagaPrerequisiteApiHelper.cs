using System.Net.Http.Json;
using System.Net;
using Catalog.API.Application.Showtimes.Commands.CreateShowtime;
using Seat.API.Application.Command.LockSeat;
using Seat.API.Domain.Entities;
using Seat.API.Domain.Interfaces;

namespace Booking.Saga.IntegrationTests;

public sealed record SagaPrerequisiteResult(
    int ShowtimeId,
    Guid ReservationId,
    string UserId,
    IReadOnlyCollection<string> SeatIds,
    decimal TotalPrice,
    int ReservationVersion);

public static class SagaPrerequisiteApiHelper
{
    public static async Task<SagaPrerequisiteResult> CreateShowtimeAndLockSeatsAsync(
        HttpClient catalogApi,
        HttpClient seatApi,
        ISeatRepository seatRepository,
        IRedisLockService redisLockService,
        CreateShowtimeCommand createShowtime,
        string userId,
        int seatCount = 2,
        TimeSpan? seatMapTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogApi);
        ArgumentNullException.ThrowIfNull(seatApi);
        ArgumentNullException.ThrowIfNull(seatRepository);
        ArgumentNullException.ThrowIfNull(redisLockService);

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User id is required.", nameof(userId));

        if (seatCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(seatCount), "Seat count must be greater than zero.");

        var createResponse = await catalogApi.PostAsJsonAsync(
            "/api/v1/catalog/showtimes",
            createShowtime,
            cancellationToken);

        await EnsureSuccessAsync(createResponse, "create showtime", cancellationToken);

        var createdShowtime = await createResponse.Content.ReadFromJsonAsync<CreateShowtimeResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Catalog API returned an empty create-showtime response.");

        if (createdShowtime.Id <= 0)
            throw new InvalidOperationException("Catalog API returned an invalid showtime id.");

        var seatMap = await WaitForSeatMapAsync(
            seatApi,
            createdShowtime.Id,
            seatMapTimeout ?? TimeSpan.FromSeconds(15),
            cancellationToken);

        var seatsToLock = seatMap.Seats
            .Where(seat => seat.SeatStatus == SeatStatus.Available)
            .Take(seatCount)
            .ToArray();

        if (seatsToLock.Length != seatCount)
        {
            throw new InvalidOperationException(
                $"Showtime {createdShowtime.Id} has only {seatsToLock.Length} available seats; {seatCount} are required.");
        }

        foreach (var seat in seatsToLock)
        {
            var lockResponse = await seatApi.PostAsJsonAsync(
                "/api/v1/seat/lock",
                new LockSeatCommand(createdShowtime.Id, seat.SeatId, userId),
                cancellationToken);

            await EnsureSuccessAsync(
                lockResponse,
                $"lock seat {seat.SeatId} for showtime {createdShowtime.Id}",
                cancellationToken);
        }

        var reservation = await seatRepository.GetSeatReservationHashAsync(createdShowtime.Id, userId)
            ?? throw new InvalidOperationException(
                $"Redis reservation was not created for showtime {createdShowtime.Id} and user {userId}.");

        var expectedSeatIds = seatsToLock
            .Select(seat => seat.SeatId)
            .OrderBy(seatId => seatId, StringComparer.Ordinal)
            .ToArray();

        var reservedSeatIds = reservation.SeatIds
            .OrderBy(seatId => seatId, StringComparer.Ordinal)
            .ToArray();

        if (!expectedSeatIds.SequenceEqual(reservedSeatIds, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Redis reservation seats [{string.Join(", ", reservedSeatIds)}] do not match locked seats [{string.Join(", ", expectedSeatIds)}].");
        }

        if (reservation.Id == Guid.Empty || reservation.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Redis reservation id or expiration is invalid.");

        foreach (var seatId in expectedSeatIds)
        {
            var persistedSeat = await seatRepository.GetSeatHashAsync(createdShowtime.Id, seatId);
            var persistedLock = await redisLockService.GetLockSeatAsync(createdShowtime.Id, seatId);

            if (persistedSeat is null ||
                persistedSeat.SeatStatus != SeatStatus.Locked ||
                !string.Equals(persistedSeat.LockedByUserId, userId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Redis seat map does not contain locked seat {seatId} for user {userId}.");
            }

            if (persistedLock is null ||
                !string.Equals(persistedLock.LockedByUserId, userId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(persistedLock.LockToken))
            {
                throw new InvalidOperationException(
                    $"Redis lock key is missing or invalid for seat {seatId}.");
            }
        }

        return new SagaPrerequisiteResult(
            createdShowtime.Id,
            reservation.Id,
            userId,
            expectedSeatIds,
            seatsToLock.Sum(seat => seat.BasePrice),
            reservation.ReservationVersion);
    }

    private static async Task<ShowtimeSeat> WaitForSeatMapAsync(
        HttpClient seatApi,
        int showtimeId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var response = await seatApi.GetAsync(
                $"/api/v1/seat/{showtimeId}/map",
                cancellationToken);

            await EnsureSuccessAsync(response, $"get seat map for showtime {showtimeId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            var seatMap = await response.Content.ReadFromJsonAsync<ShowtimeSeat>(
                cancellationToken: cancellationToken);

            if (seatMap?.Seats?.Any() == true)
                return seatMap;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException(
            $"Seat map for showtime {showtimeId} was not available after {timeout}. " +
            "Verify that the ShowtimeCreatedIntegrationEvent consumer is running.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Could not {operation}. HTTP {(int)response.StatusCode}: {responseBody}");
    }

    private sealed record CreateShowtimeResponse(int Id);
}
