#!/usr/bin/env bash
set -euo pipefail

CATALOG_URL="${CATALOG_URL:-http://localhost:8081}"
SEAT_URL="${SEAT_URL:-http://localhost:8082}"
BOOKING_URL="${BOOKING_URL:-http://localhost:8083}"


SEAT_MAP_TIMEOUT_SECONDS="${SEAT_MAP_TIMEOUT_SECONDS:-30}"
PAYMENT_TIMEOUT_SECONDS="${PAYMENT_TIMEOUT_SECONDS:-45}"
SMOKE_USER_ID="${SMOKE_USER_ID:-smoke-user-$(date +%s)}"
SMOKE_USER_NAME="${SMOKE_USER_NAME:-Smoke Test User}"

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

request_json() {
  local name="$1"
  local method="$2"
  local url="$3"
  local body_file="$4"
  local expected_status="$5"
  local response_file="$6"

  printf 'Calling %s: %s %s\n' "$name" "$method" "$url"

  local status
  status="$(
    curl -sS \
      -X "$method" \
      -H 'Content-Type: application/json' \
      -d @"$body_file" \
      -o "$response_file" \
      -w '%{http_code}' \
      "$url"
  )"

  if [[ "$status" != "$expected_status" ]]; then
    printf 'Smoke happy path failed: %s returned HTTP %s, expected %s\n' "$name" "$status" "$expected_status" >&2
    printf 'Response body:\n' >&2
    cat "$response_file" >&2
    printf '\n' >&2
    exit 1
  fi
}

request_json_allow_statuses() {
  local name="$1"
  local method="$2"
  local url="$3"
  local body_file="$4"
  local response_file="$5"
  shift 5
  local expected_statuses=("$@")

  printf 'Calling %s: %s %s\n' "$name" "$method" "$url"

  local status
  status="$(
    curl -sS \
      -X "$method" \
      -H 'Content-Type: application/json' \
      -d @"$body_file" \
      -o "$response_file" \
      -w '%{http_code}' \
      "$url"
  )"

  for expected_status in "${expected_statuses[@]}"; do
    if [[ "$status" == "$expected_status" ]]; then
      return 0
    fi
  done

  printf 'Smoke happy path failed: %s returned HTTP %s, expected one of: %s\n' "$name" "$status" "${expected_statuses[*]}" >&2
  printf 'Response body:\n' >&2
  cat "$response_file" >&2
  printf '\n' >&2
  exit 1
}

request_get() {
  local name="$1"
  local url="$2"
  local expected_status="$3"
  local response_file="$4"

  printf 'Calling %s: GET %s\n' "$name" "$url"

  local status
  status="$(
    curl -sS \
      -o "$response_file" \
      -w '%{http_code}' \
      "$url"
  )"

  if [[ "$status" != "$expected_status" ]]; then
    printf 'Smoke happy path failed: %s returned HTTP %s, expected %s\n' "$name" "$status" "$expected_status" >&2
    printf 'Response body:\n' >&2
    cat "$response_file" >&2
    printf '\n' >&2
    exit 1
  fi
}

extract_id() {
  local response_file="$1"
  sed -nE 's/.*"id"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' "$response_file" | head -n 1
}

extract_guid() {
  local response_file="$1"
  sed -nE 's/.*"id"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/p' "$response_file" | head -n 1
}

extract_booking_id() {
  local response_file="$1"
  sed -nE 's/.*"id"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' "$response_file" | head -n 1
}

extract_first_seat_id() {
  local response_file="$1"
  sed -nE 's/.*"seatId"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/p' "$response_file" | head -n 1
}

wait_for_seat_map() {
  local showtime_id="$1"
  local response_file="$2"
  local deadline=$((SECONDS + SEAT_MAP_TIMEOUT_SECONDS))
  local status

  printf 'Waiting for Seat map from integration event: showtime %s\n' "$showtime_id"

  while (( SECONDS < deadline )); do
    status="$(
      curl -sS \
        -o "$response_file" \
        -w '%{http_code}' \
        "$SEAT_URL/api/v1/seat/$showtime_id/map" || true
    )"

    if [[ "$status" == "200" ]] && grep -q '"seatId"' "$response_file"; then
      return 0
    fi

    sleep 2
  done

  printf 'Smoke happy path failed: seat map was not ready for showtime %s after %s seconds.\n' "$showtime_id" "$SEAT_MAP_TIMEOUT_SECONDS" >&2
  printf 'Last response body:\n' >&2
  cat "$response_file" >&2
  printf '\n' >&2
  exit 1
}

wait_for_booking_status() {
  local booking_id="$1"
  local expected_status="$2"
  local response_file="$3"
  local deadline=$((SECONDS + PAYMENT_TIMEOUT_SECONDS))

  printf 'Waiting for Booking %s status: %s\n' "$booking_id" "$expected_status"

  while (( SECONDS < deadline )); do
    request_get \
      "Get booking by id" \
      "$BOOKING_URL/api/booking/$booking_id?api-version=1.0" \
      "200" \
      "$response_file"

    if grep -q "\"bookingStatus\"[[:space:]]*:[[:space:]]*\"$expected_status\"" "$response_file"; then
      return 0
    fi

    sleep 2
  done

  printf 'Smoke happy path failed: booking %s did not reach status %s after %s seconds.\n' "$booking_id" "$expected_status" "$PAYMENT_TIMEOUT_SECONDS" >&2
  printf 'Last response body:\n' >&2
  cat "$response_file" >&2
  printf '\n' >&2
  exit 1
}

MOVIE_BODY="$TMP_DIR/create-movie.json"
MOVIE_RESPONSE="$TMP_DIR/create-movie-response.json"
SHOWTIME_BODY="$TMP_DIR/create-showtime.json"
SHOWTIME_RESPONSE="$TMP_DIR/create-showtime-response.json"
SEAT_MAP_RESPONSE="$TMP_DIR/seat-map-response.json"
LOCK_SEAT_BODY="$TMP_DIR/lock-seat.json"
LOCK_SEAT_RESPONSE="$TMP_DIR/lock-seat-response.json"
VALIDATION_BODY="$TMP_DIR/validation-reservation.json"
VALIDATION_RESPONSE="$TMP_DIR/validation-reservation-response.json"
CREATE_BOOKING_BODY="$TMP_DIR/create-booking-from-reservation.json"
CREATE_BOOKING_RESPONSE="$TMP_DIR/create-booking-from-reservation-response.txt"
GET_BOOKINGS_RESPONSE="$TMP_DIR/get-bookings-by-user-response.json"
PAYMENT_BODY="$TMP_DIR/set-awaiting-payment.json"
PAYMENT_RESPONSE="$TMP_DIR/set-awaiting-payment-response.txt"
GET_BOOKING_RESPONSE="$TMP_DIR/get-booking-response.json"

cat > "$MOVIE_BODY" <<'JSON'
{
  "tiltle": "Smoke Test Movie",
  "description": "Movie created by smoke happy path.",
  "durationMinutes": 120,
  "releaseDate": "2024-01-01T00:00:00Z",
  "director": "Smoke Test Director",
  "cast": "Smoke Test Cast",
  "trailerUrl": "https://example.com/trailer",
  "posterUrl": "https://example.com/poster"
}
JSON

# request_json \
#   "Create movie" \
#   "POST" \
#   "$CATALOG_URL/api/v1/catalog/movies" \
#   "$MOVIE_BODY" \
#   "201" \
#   "$MOVIE_RESPONSE"

# MOVIE_ID="$(extract_id "$MOVIE_RESPONSE")"

# if [[ -z "$MOVIE_ID" ]]; then
#   printf 'Smoke happy path failed: could not extract movie id.\n' >&2
#   cat "$MOVIE_RESPONSE" >&2
#   printf '\n' >&2
#   exit 1
# fi

# printf 'Created movie id: %s\n' "$MOVIE_ID"

cat > "$SHOWTIME_BODY" <<JSON
{
  "movieId": 1,
  "cinemaId": 1,
  "hallId": 1,
  "startTime": "2026-01-01T18:00:00Z",
  "endTime": "2026-01-01T20:00:00Z",
  "basePrice": 90000
}
JSON

request_json \
  "Create showtime" \
  "POST" \
  "$CATALOG_URL/api/v1/catalog/showtimes" \
  "$SHOWTIME_BODY" \
  "201" \
  "$SHOWTIME_RESPONSE"

SHOWTIME_ID="$(extract_id "$SHOWTIME_RESPONSE")"

if [[ -z "$SHOWTIME_ID" ]]; then
  printf 'Smoke happy path failed: could not extract showtime id.\n' >&2
  cat "$SHOWTIME_RESPONSE" >&2
  printf '\n' >&2
  exit 1
fi

printf 'Created showtime id: %s\n' "$SHOWTIME_ID"
wait_for_seat_map "$SHOWTIME_ID" "$SEAT_MAP_RESPONSE"
printf 'Seat map created for showtime id: %s\n' "$SHOWTIME_ID"

SEAT_ID="$(extract_first_seat_id "$SEAT_MAP_RESPONSE")"

if [[ -z "$SEAT_ID" ]]; then
  printf 'Smoke happy path failed: could not extract seat id from seat map.\n' >&2
  cat "$SEAT_MAP_RESPONSE" >&2
  printf '\n' >&2
  exit 1
fi

printf 'Using seat id: %s\n' "$SEAT_ID"

cat > "$LOCK_SEAT_BODY" <<JSON
{
  "showtimeId": $SHOWTIME_ID,
  "seatId": "$SEAT_ID",
  "userId": "$SMOKE_USER_ID"
}
JSON

request_json \
  "Lock seat" \
  "POST" \
  "$SEAT_URL/api/v1/seat/lock" \
  "$LOCK_SEAT_BODY" \
  "200" \
  "$LOCK_SEAT_RESPONSE"

cat > "$VALIDATION_BODY" <<JSON
{
  "showtimeId": $SHOWTIME_ID,
  "reservationId": "",
  "userId": "$SMOKE_USER_ID"
}
JSON

request_json \
  "Validate reservation" \
  "PUT" \
  "$SEAT_URL/api/v1/seat/validation-reservation" \
  "$VALIDATION_BODY" \
  "200" \
  "$VALIDATION_RESPONSE"

RESERVATION_ID="$(extract_guid "$VALIDATION_RESPONSE")"

if [[ -z "$RESERVATION_ID" ]]; then
  printf 'Smoke happy path failed: could not extract reservation id.\n' >&2
  cat "$VALIDATION_RESPONSE" >&2
  printf '\n' >&2
  exit 1
fi

printf 'Validated reservation id: %s\n' "$RESERVATION_ID"

cat > "$CREATE_BOOKING_BODY" <<JSON
{
  "showtimeId": $SHOWTIME_ID,
  "userId": "$SMOKE_USER_ID",
  "userName": "$SMOKE_USER_NAME",
  "reservationId": "$RESERVATION_ID",
  "bookingItem": []
}
JSON

request_json_allow_statuses \
  "Create booking from reservation" \
  "POST" \
  "$BOOKING_URL/api/v1/booking/from-reservation" \
  "$CREATE_BOOKING_BODY" \
  "$CREATE_BOOKING_RESPONSE" \
  "200"

request_get \
  "Get booking by user" \
  "$BOOKING_URL/api/v1/booking/$SMOKE_USER_ID" \
  "200" \
  "$GET_BOOKINGS_RESPONSE"

if ! grep -q '"bookingStatus"' "$GET_BOOKINGS_RESPONSE"; then
  printf 'Smoke happy path failed: booking response does not contain bookingStatus.\n' >&2
  cat "$GET_BOOKINGS_RESPONSE" >&2
  printf '\n' >&2
  exit 1
fi

BOOKING_ID="$(extract_booking_id "$GET_BOOKINGS_RESPONSE")"

if [[ -z "$BOOKING_ID" ]]; then
  printf 'Smoke happy path failed: could not extract booking id.\n' >&2
  cat "$GET_BOOKINGS_RESPONSE" >&2
  printf '\n' >&2
  exit 1
fi

printf 'Booking created for user: %s, booking id: %s\n' "$SMOKE_USER_ID" "$BOOKING_ID"

cat > "$PAYMENT_BODY" <<JSON
{
  "bookingId": $BOOKING_ID
}
JSON

request_json \
  "Set booking awaiting payment" \
  "PUT" \
  "$BOOKING_URL/api/v1/booking/payment" \
  "$PAYMENT_BODY" \
  "200" \
  "$PAYMENT_RESPONSE"

wait_for_booking_status "$BOOKING_ID" "paid" "$GET_BOOKING_RESPONSE"
printf 'Payment completed and booking is paid: %s\n' "$BOOKING_ID"
printf 'Smoke happy path passed.\n'
