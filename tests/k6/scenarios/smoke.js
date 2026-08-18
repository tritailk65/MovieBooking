import { bookingHappyPath } from '../flows/booking-happy-path.js';
import { happyPathSmokeThresholds } from '../config/thresholds.js';

export const options = {
  scenarios: {
    booking_smoke: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '2m',
      exec: 'bookingSmoke',
    },
  },
  thresholds: happyPathSmokeThresholds,
};

export function bookingSmoke() {
  bookingHappyPath();
}
