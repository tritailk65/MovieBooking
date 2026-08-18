import { check, fail, group } from 'k6';
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
  getBookingSagaForPolling,
  getBookingById
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

function requireEqual(actual, expected, message) {
  const passed = check(actual, {
    [message]: (value) => value === expected,
  });

  if (!passed) {
    fail(
      `${message}. Expected: ${expected}. Actual: ${actual}`,
    );
  }

  return actual;
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
    const createdBooking = createBookingFromReservation({
      showtimeId,
      userId: identity.userId,
      userName: identity.userName,
      reservationId,
    });

    bookingId = requireValue(
      createdBooking && createdBooking.bookingId,
      'Create booking response contains bookingId',
    );

    const createdReservationId = requireValue(
      createdBooking && createdBooking.reservationId,
      'Create booking response contains reservationId',
    );

    const creationStatus = requireValue(
      createdBooking && createdBooking.status,
      'Create booking response contains status',
    );

    requireEqual(
      String(createdReservationId).toLowerCase(),
      String(reservationId).toLowerCase(),
      'Created booking belongs to the requested reservation',
    );

    requireEqual(
      creationStatus.toLowerCase(),
      'submitted',
      'Create booking response has Submitted status',
    );

    const booking = getBookingById(bookingId);

    requireEqual(
      booking.id,
      bookingId,
      'Loaded booking has the expected booking id',
    );

    requireEqual(
      booking.userId,
      identity.userId,
      `Booking ${bookingId} belongs to the expected user`,
    );

    requireEqual(
      booking.showtimeId,
      showtimeId,
      `Booking ${bookingId} belongs to the expected showtime`,
    );

    const bookingStatus = requireValue(
      booking.bookingStatus,
      `Booking ${bookingId} contains bookingStatus`,
    );

    requireEqual(
      bookingStatus.toLowerCase(),
      'submitted',
      `Booking ${bookingId} is initially Submitted`,
    );
  });

  group('05 - Wait for saga PendingPayment', () => {
    pollUntil({
      name:
        `Booking saga ${reservationId} reaches PendingPayment`,
      timeoutSeconds: environment.paymentTimeoutSeconds,
      intervalSeconds: environment.pollingIntervalSeconds,
      request: () =>
        getBookingSagaForPolling(reservationId),
      predicate: (response) => {
        if (!response || response.status !== 200) {
          return false;
        }

        const saga = tryParseJson(response);

        return Boolean(
          saga &&
            saga.bookingId === bookingId &&
            saga.currentState &&
            saga.currentState.toLowerCase() ===
              'pendingpayment',
        );
      },
      describeLastResult: describeHttpResponse,
    });
  });

  group('06 - Start payment and wait for paid status', () => {
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

  group('07 - Wait for saga finalization', () => {
    pollUntil({
      name: `Booking saga ${reservationId} is finalized`,
      timeoutSeconds: environment.paymentTimeoutSeconds,
      intervalSeconds: environment.pollingIntervalSeconds,
      request: () =>
        getBookingSagaForPolling(reservationId),
      predicate: (response) =>
        Boolean(response && response.status === 404),
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

