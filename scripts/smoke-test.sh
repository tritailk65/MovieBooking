#!/usr/bin/env bash
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:8080/health/ready}"
CATALOG_URL="${CATALOG_URL:-http://localhost:8081/health/ready}"
SEAT_URL="${SEAT_URL:-http://localhost:8082/health/ready}"
BOOKING_URL="${BOOKING_URL:-http://localhost:8083/health/ready}"
PAYMENT_URL="${PAYMENT_URL:-http://localhost:8084/health/ready}"

check_http() {
  local name="$1"
  local url="$2"

  printf 'Checking %s: %s\n' "$name" "$url"

  if ! curl \
    --fail \
    --silent \
    --show-error \
    --output /dev/null \
    --connect-timeout 5 \
    --max-time 15 \
    "$url"; then
    printf 'Health check failed: %s\n' "$name" >&2
    exit 1
  fi
}

check_http "Gateway health" "$GATEWAY_URL"
check_http "Catalog health" "$CATALOG_URL"
check_http "Seat health" "$SEAT_URL"
check_http "Booking health" "$BOOKING_URL"
check_http "Payment health" "$PAYMENT_URL"

printf 'All service health checks passed.\n'