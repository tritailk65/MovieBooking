function withoutTrailingSlash(value) {
  return value.replace(/\/+$/, '');
}

function numberFromEnvironment(name, fallback) {
  const rawValue = __ENV[name];
  if (rawValue === undefined || rawValue === '') {
    return fallback;
  }

  const value = Number(rawValue);
  if (!Number.isFinite(value) || value <= 0) {
    throw new Error(`${name} must be a positive number. Received: ${rawValue}`);
  }

  return value;
}

function optionalPositiveInteger(name) {
  const rawValue = __ENV[name];
  if (rawValue === undefined || rawValue === '') {
    return null;
  }

  const value = Number(rawValue);
  if (!Number.isInteger(value) || value <= 0) {
    throw new Error(`${name} must be a positive integer. Received: ${rawValue}`);
  }

  return value;
}

export const environment = Object.freeze({
  gatewayUrl: withoutTrailingSlash(
    __ENV.GATEWAY_URL || 'http://localhost:8080',
  ),
  catalogAdminUrl: withoutTrailingSlash(
    __ENV.CATALOG_ADMIN_URL || 'http://localhost:8081',
  ),
  apiVersion: __ENV.API_VERSION || '1.0',
  requestTimeout: __ENV.REQUEST_TIMEOUT || '15s',
  seatMapTimeoutSeconds: numberFromEnvironment(
    'SEAT_MAP_TIMEOUT_SECONDS',
    30,
  ),
  paymentTimeoutSeconds: numberFromEnvironment(
    'PAYMENT_TIMEOUT_SECONDS',
    45,
  ),
  pollingIntervalSeconds: numberFromEnvironment(
    'POLLING_INTERVAL_SECONDS',
    2,
  ),
  userPrefix: __ENV.TEST_USER_PREFIX || 'k6-user',
  userName: __ENV.TEST_USER_NAME || 'K6 Test User',
  testRunId: __ENV.TEST_RUN_ID || `${Date.now()}`,
  existingShowtimeId: optionalPositiveInteger('SHOWTIME_ID'),
  movieId: numberFromEnvironment('MOVIE_ID', 1),
  cinemaId: numberFromEnvironment('CINEMA_ID', 1),
  hallId: numberFromEnvironment('HALL_ID', 1),
  basePrice: numberFromEnvironment('BASE_PRICE', 90000),
  showtimeStartOffsetMinutes: numberFromEnvironment(
    'SHOWTIME_START_OFFSET_MINUTES',
    60,
  ),
  showtimeDurationMinutes: numberFromEnvironment(
    'SHOWTIME_DURATION_MINUTES',
    120,
  ),
  verbose: (__ENV.VERBOSE || '').toLowerCase() === 'true',
});

// Backwards-compatible named exports for small, standalone scenarios.
export const gatewayUrl = environment.gatewayUrl;
export const requestTimeout = environment.requestTimeout;
export const paymentTimeoutSeconds = environment.paymentTimeoutSeconds;
