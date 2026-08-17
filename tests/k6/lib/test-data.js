import exec from 'k6/execution';
import { environment } from '../config/environment.js';

function iterationSuffix() {
  return [
    environment.testRunId,
    exec.vu.idInTest,
    exec.scenario.iterationInTest,
  ].join('-');
}

export function createTestIdentity() {
  const suffix = iterationSuffix();

  return {
    userId: `${environment.userPrefix}-${suffix}`,
    userName: `${environment.userName} ${suffix}`,
  };
}

export function createShowtimePayload() {
  const uniqueOffsetSeconds =
    exec.vu.idInTest * 1000 + exec.scenario.iterationInTest;
  const startTime = new Date(
    Date.now() +
      environment.showtimeStartOffsetMinutes * 60 * 1000 +
      uniqueOffsetSeconds * 1000,
  );
  const endTime = new Date(
    startTime.getTime() + environment.showtimeDurationMinutes * 60 * 1000,
  );

  return {
    movieId: environment.movieId,
    cinemaId: environment.cinemaId,
    hallId: environment.hallId,
    startTime: startTime.toISOString(),
    endTime: endTime.toISOString(),
    basePrice: environment.basePrice,
  };
}
