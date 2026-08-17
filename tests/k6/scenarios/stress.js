import { bookingHappyPath } from '../flows/booking-happy-path.js';
import { stressThresholds } from '../config/thresholds.js';
import { workloads } from '../config/workloads.js';

export const options = {
  scenarios: {
    booking_stress: {
      executor: 'ramping-vus',
      stages: [
        {
          duration: workloads.stress.stageDuration,
          target: workloads.stress.firstTargetVus,
        },
        {
          duration: workloads.stress.stageDuration,
          target: workloads.stress.secondTargetVus,
        },
        {
          duration: workloads.stress.stageDuration,
          target: workloads.stress.finalTargetVus,
        },
        {
          duration: workloads.stress.stageDuration,
          target: 0,
        },
      ],
      gracefulRampDown: '30s',
      gracefulStop: '2m',
      exec: 'bookingStress',
    },
  },
  thresholds: stressThresholds,
};

export function bookingStress() {
  bookingHappyPath();
}
