import { group } from 'k6';
import { environment } from '../config/environment.js';
import { createShowtime } from '../api/catalog-api.js';
import {
  extractSeats,
  getSeatMapForPolling,
  getSeatReservation,
  lockSeat,
} from '../api/seat-api.js';
import {
  createBookingFromReservation,
  getBookingForPolling,
  getBookingsByUser,
  setBookingAwaitingPayment,
} from '../api/booking-api.js';
import {
  requireValue,
  tryParseJson,
} from '../lib/checks.js';
import { pollUntil } from '../lib/polling.js';
import {
  createShowtimePayload,
  createTestIdentity,
} from '../lib/test-data.js';

function isAvailableSeat(seat) {
  if (
    !seat ||
    seat.seatStatus === undefined ||
    seat.seatStatus === null
  ) {
    return true;
  }

  return (
    seat.seatStatus === 1 ||
    String(seat.seatStatus).toLowerCase() === 'available'
  );
}

function describeHttpResponse(response) {
  if (!response) {
    return '<no HTTP response>';
  }

  const body = String(response.body || '');
  const preview = body.length <= 300 ? body : `${body.slice(0, 300)}...`;
  return `HTTP ${response.status}: ${preview || '<empty body>'}`;
}

function bookingCollection(responseBody) {
  if (Array.isArray(responseBody)) {
    return responseBody;
  }

  if (responseBody && Array.isArray(responseBody.items)) {
    return responseBody.items;
  }

  return [];
}

export function bookingHappyPath() {
  const identity = createTestIdentity();
  let showtimeId = environment.existingShowtimeId;
  let selectedSeat;
  let reservationId;
  let bookingId;

  if (!showtimeId) {
    group('01 - Create showtime test data', () => {
      const response = createShowtime(createShowtimePayload());
      showtimeId = requireValue(
        response && response.id,
        'Create showtime response contains id',
      );
    });
  }

  group('02 - Wait for seat map integration event', () => {
    const response = pollUntil({
      name: `Seat map for showtime ${showtimeId}`,
      timeoutSeconds: environment.seatMapTimeoutSeconds,
      intervalSeconds: environment.pollingIntervalSeconds,
      request: () => getSeatMapForPolling(showtimeId),
      predicate: (current) => extractSeats(current).length > 0,
      describeLastResult: describeHttpResponse,
    });

    const seats = extractSeats(response);
    selectedSeat = requireValue(
      seats.find(isAvailableSeat),
      `Seat map for showtime ${showtimeId} contains an available seat`,
    );
    requireValue(
      selectedSeat.seatId,
      `Selected seat for showtime ${showtimeId} contains seatId`,
    );
  });

  group('03 - Lock seat and load reservation', () => {
    lockSeat({
      showtimeId,
      seatId: selectedSeat.seatId,
      userId: identity.userId,
    });

    // The old shell script called the internal validation endpoint with an
    // empty reservation id to discover the generated id. The public Gateway
    // already exposes this read endpoint, while Booking validates the same
    // reservation over gRPC when creating the booking.
    const reservation = getSeatReservation({
      showtimeId,
      userId: identity.userId,
    });

    reservationId = requireValue(
      reservation && reservation.id,
      'Seat reservation response contains id',
    );
  });

  group('04 - Create and verify booking', () => {
    const createResponse = createBookingFromReservation({
      showtimeId,
      userId: identity.userId,
      userName: identity.userName,
      reservationId,
    });

    const createdBooking = tryParseJson(createResponse);
    const createdBookingId = createdBooking && createdBooking.bookingId;
    const bookings = bookingCollection(
      getBookingsByUser(identity.userId),
    );
    const booking = bookings.find(
      (candidate) => candidate.id === createdBookingId,
    ) || bookings[0];

    bookingId = requireValue(
      createdBookingId || (booking && booking.id),
      'Booking response contains booking id',
    );
    requireValue(
      booking && booking.bookingStatus,
      `Booking ${bookingId} contains bookingStatus`,
    );
  });

  group('05 - Start payment and wait for paid status', () => {
    setBookingAwaitingPayment(bookingId);

    pollUntil({
      name: `Booking ${bookingId} reaches paid status`,
      timeoutSeconds: environment.paymentTimeoutSeconds,
      intervalSeconds: environment.pollingIntervalSeconds,
      request: () => getBookingForPolling(bookingId),
      predicate: (response) => {
        if (!response || response.status !== 200) {
          return false;
        }

        const booking = tryParseJson(response);
        return Boolean(
          booking &&
            booking.bookingStatus &&
            booking.bookingStatus.toLowerCase() === 'paid',
        );
      },
      describeLastResult: describeHttpResponse,
    });
  });

  if (environment.verbose) {
    console.log(
      `Happy path passed: user=${identity.userId}, ` +
        `showtime=${showtimeId}, seat=${selectedSeat.seatId}, ` +
        `reservation=${reservationId}, booking=${bookingId}`,
    );
  }

  return {
    userId: identity.userId,
    showtimeId,
    seatId: selectedSeat.seatId,
    reservationId,
    bookingId,
  };
}
