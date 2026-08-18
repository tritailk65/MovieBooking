import { environment } from '../config/environment.js';
import {
  getForPolling,
  requestJson,
  requestWithoutJsonResponse,
} from '../lib/http.js';

// function apiVersionQuery() {
//   return `api-version=${encodeURIComponent(environment.apiVersion)}`;
// }

export function createBookingFromReservation({
  showtimeId,
  userId,
  userName,
  reservationId,
}) {
  return requestJson({
    name: 'Create booking from reservation',
    method: 'POST',
    url:
      `${environment.gatewayUrl}/api/v1/booking/from-reservation`,
    body: {
      showtimeId,
      userId,
      userName,
      reservationId,
      bookingItem: [],
    },
    // The current API returns JSON with 201. Accepting the former 200/text
    // response keeps this test usable during a rolling deployment; the next
    // request resolves the booking id from the user query in either case.
    expectedStatuses: 201,
    service: 'booking',
    operation: 'create_booking',
  });
}

export function getBookingsByUser(userId) {
  return requestJson({
    name: 'Get bookings by user',
    method: 'GET',
    url:
      `${environment.gatewayUrl}/api/v1/booking/${encodeURIComponent(userId)}`,
    expectedStatuses: 200,
    service: 'booking',
    operation: 'get_bookings_by_user',
  });
}

export function setBookingAwaitingPayment(bookingId) {
  return requestWithoutJsonResponse({
    name: 'Set booking awaiting payment',
    method: 'PUT',
    url:
      `${environment.gatewayUrl}/api/v1/booking/payment`,
    body: { bookingId },
    expectedStatuses: 200,
    service: 'booking',
    operation: 'start_payment',
  });
}

export function getBookingForPolling(bookingId) {
  return getForPolling({
    url:
      `${environment.gatewayUrl}/api/v1/booking/${bookingId}` ,
      // apiVersionQuery(),
    service: 'booking',
    operation: 'wait_for_paid_booking',
  });
}


// Helper for check state of saga
export function getBookingSagaForPolling(reservationId) {
  return getForPolling({
    url:
      `${environment.gatewayUrl}/api/v1/booking/saga/` +
      `${encodeURIComponent(reservationId)}`,
    service: 'booking',
    operation: 'wait_for_booking_saga',
  });
}


export function getBookingById(bookingId) {
  return requestJson({
    name: `Get booking ${bookingId}`,
    method: 'GET',
    url:
      `${environment.gatewayUrl}/api/v1/booking/${bookingId}`,
    expectedStatuses: 200,
    service: 'booking',
    operation: 'get_booking_by_id',
  });
}