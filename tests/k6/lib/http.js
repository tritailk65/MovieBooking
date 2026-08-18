import http from 'k6/http';
import { environment } from '../config/environment.js';
import { expectStatus, parseJson } from './checks.js';

function requestParameters(service, operation, phase, extraParameters = {}) {
  // const correlationId =
  //   `k6-${environment.testRunId}-${operation}-${__VU}-${__ITER}`;

  const correlationId = `k6-${environment.testRunId}-${__VU}-${__ITER}`;

  return {
    timeout: environment.requestTimeout,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-Correlation-Id': correlationId,
      ...(extraParameters.headers || {}),
    },
    tags: {
      service,
      operation,
      phase,
      ...(extraParameters.tags || {}),
    },
    ...extraParameters,
  };
}

export function requestJson({
  name,
  method,
  url,
  body,
  expectedStatuses,
  service,
  operation,
  phase = 'business',
}) {
  const response = http.request(
    method,
    url,
    body === undefined || body === null ? null : JSON.stringify(body),
    requestParameters(service, operation, phase),
  );

  expectStatus(response, expectedStatuses, name);
  return parseJson(response, name);
}

export function requestWithoutJsonResponse({
  name,
  method,
  url,
  body,
  expectedStatuses,
  service,
  operation,
  phase = 'business',
}) {
  const response = http.request(
    method,
    url,
    body === undefined || body === null ? null : JSON.stringify(body),
    requestParameters(service, operation, phase),
  );

  expectStatus(response, expectedStatuses, name);
  return response;
}

export function getForPolling({ url, service, operation }) {
  return http.get(
    url,
    requestParameters(service, operation, 'polling'),
  );
}
