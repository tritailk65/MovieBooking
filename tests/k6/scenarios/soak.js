import { bookingHappyPath } from '../flows/booking-happy-path.js';
import { soakThresholds } from '../config/thresholds.js';
import { workloads } from '../config/workloads.js';

export const options = {
  scenarios: {
    booking_soak: {
      executor: 'constant-vus',
      vus: workloads.soak.vus,
      duration: workloads.soak.duration,
      gracefulStop: '2m',
      exec: 'bookingSoak',
    },
  },
  thresholds: soakThresholds,
};

export function bookingSoak() {
  bookingHappyPath();
}
