import { environment } from '../config/environment.js';
import { requestJson, getForPolling } from '../lib/http.js';
import { tryParseJson } from '../lib/checks.js';

export function getSeatMapForPolling(showtimeId) {
  return getForPolling({
    url: `${environment.gatewayUrl}/api/v1/seat/${showtimeId}/map`,
    service: 'seat',
    operation: 'wait_for_seat_map',
  });
}

export function extractSeats(response) {
  if (!response || response.status !== 200) {
    return [];
  }

  const body = tryParseJson(response);
  if (Array.isArray(body)) {
    return body;
  }

  return body && Array.isArray(body.seats) ? body.seats : [];
}

export function lockSeat({ showtimeId, seatId, userId }) {
  return requestJson({
    name: 'Lock seat',
    method: 'POST',
    url: `${environment.gatewayUrl}/api/v1/seat/lock`,
    body: {
      showtimeId,
      seatId,
      userId,
    },
    expectedStatuses: 200,
    service: 'seat',
    operation: 'lock_seat',
  });
}

export function getSeatReservation({ showtimeId, userId }) {
  const query =
    `showtimeId=${encodeURIComponent(showtimeId)}` +
    `&userId=${encodeURIComponent(userId)}`;

  return requestJson({
    name: 'Get seat reservation',
    method: 'GET',
    url: `${environment.gatewayUrl}/api/v1/seat/reservation?${query}`,
    expectedStatuses: 200,
    service: 'seat',
    operation: 'get_seat_reservation',
  });
}
