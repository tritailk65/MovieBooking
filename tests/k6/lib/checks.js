import { check, fail } from 'k6';

function responsePreview(response) {
  if (!response || response.body === null || response.body === undefined) {
    return '<empty response body>';
  }

  const body = String(response.body);
  return body.length <= 500 ? body : `${body.slice(0, 500)}...`;
}

export function expectStatus(response, expectedStatuses, operation) {
  const statuses = Array.isArray(expectedStatuses)
    ? expectedStatuses
    : [expectedStatuses];

  const passed = check(response, {
    [`${operation}: expected HTTP ${statuses.join(' or ')}`]: (result) =>
      statuses.includes(result.status),
  });

  if (!passed) {
    const correlationId = response.headers['X-Correlation-Id'];
        
    fail(
      `${operation} returned HTTP ${response.status}. ` +
        `CorrelationId: ${correlationId}. ` +
        `Response: ${responsePreview(response)}`,
    );
  }

  return response;
}

export function parseJson(response, operation) {
  try {
    return response.json();
  } catch (error) {
    fail(
      `${operation} did not return valid JSON. ` +
        `Response: ${responsePreview(response)}. Error: ${error.message}`,
    );
    return null;
  }
}

export function tryParseJson(response) {
  if (!response || response.body === null || response.body === '') {
    return null;
  }

  try {
    return response.json();
  } catch {
    return null;
  }
}

export function requireValue(value, message) {
  const present = check(value, {
    [message]: (current) =>
      current !== null &&
      current !== undefined &&
      current !== '',
  });

  if (!present) {
    fail(message);
  }

  return value;
}
