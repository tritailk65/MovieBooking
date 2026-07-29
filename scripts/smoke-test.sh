#!/usr/bin/env bash
set -euo pipefail

CATALOG_URL="${CATALOG_URL:-http://localhost:8081}"
SEAT_URL="${SEAT_URL:-http://localhost:8082}"
BOOKING_URL="${BOOKING_URL:-http://localhost:8083}"
PAYMENT_URL="${PAYMENT_URL:-http://localhost:8084}"
RABBITMQ_URL="${RABBITMQ_URL:-http://localhost:15672}"

check_http() {
  local name="$1"
  local url="$2"

  printf 'Checking %s: %s\n' "$name" "$url"

  local status
  status="$(curl -fsS -o /dev/null -w '%{http_code}' "$url")"

  if [[ "$status" != "200" ]]; then
    printf 'Smoke test failed: %s returned HTTP %s\n' "$name" "$status" >&2
    exit 1
  fi
}

check_http "Catalog health" "$CATALOG_URL/health"
check_http "Seat health" "$SEAT_URL/health"
check_http "Booking health" "$BOOKING_URL/health"
check_http "Payment health" "$PAYMENT_URL/health"
check_http "RabbitMQ management" "$RABBITMQ_URL"

printf 'Smoke tests passed.\n'
