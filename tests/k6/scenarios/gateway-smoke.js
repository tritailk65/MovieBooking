import http from 'k6/http';
import { check, group } from 'k6';
import { environment } from '../config/environment.js';
import { gatewaySmokeThresholds } from '../config/thresholds.js';

export const options = {
  scenarios: {
    gateway_smoke: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '30s',
    },
  },
  thresholds: gatewaySmokeThresholds,
  discardResponseBodies: true,
};

function checkEndpoint(name, path) {
  group(name, () => {
    const response = http.get(`${environment.gatewayUrl}${path}`, {
      timeout: environment.requestTimeout,
      tags: {
        service: 'gateway',
        operation: name,
        phase: 'business',
      },
    });

    check(response, {
      [`${name} returns HTTP 200`]: (result) => result.status === 200,
    });
  });
}

export default function () {
  checkEndpoint('gateway_readiness', '/health/ready');
  checkEndpoint('catalog_movies', '/api/v1/catalog/movies');
  checkEndpoint('seat_map', '/api/v1/seat/0/map');
  checkEndpoint('booking_card_types', '/api/v1/booking/cardtype');
}
