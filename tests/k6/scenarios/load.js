import { bookingHappyPath } from '../flows/booking-happy-path.js';
import { loadThresholds } from '../config/thresholds.js';
import { workloads } from '../config/workloads.js';

export const options = {
  scenarios: {
    booking_load: {
      executor: 'ramping-vus',
      stages: [
        { duration: workloads.load.rampUp, target: workloads.load.targetVus },
        { duration: workloads.load.steady, target: workloads.load.targetVus },
        { duration: workloads.load.rampDown, target: 0 },
      ],
      gracefulRampDown: '30s',
      gracefulStop: '2m',
      exec: 'bookingLoad',
    },
  },
  thresholds: loadThresholds,
};

export function bookingLoad() {
  bookingHappyPath();
}
