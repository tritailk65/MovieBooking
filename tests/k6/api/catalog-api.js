import { environment } from '../config/environment.js';
import { requestJson } from '../lib/http.js';

export function getMovies() {
  return requestJson({
    name: 'Get movies',
    method: 'GET',
    url: `${environment.gatewayUrl}/api/v1/catalog/movies`,
    expectedStatuses: 200,
    service: 'catalog',
    operation: 'get_movies',
  });
}

export function createShowtime(payload) {
  return requestJson({
    name: 'Create showtime',
    method: 'POST',
    url: `${environment.catalogAdminUrl}/api/v1/catalog/showtimes`,
    body: payload,
    expectedStatuses: 201,
    service: 'catalog',
    operation: 'create_showtime',
    phase: 'test-data',
  });
}
