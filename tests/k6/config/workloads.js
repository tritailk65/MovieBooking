function positiveInteger(name, fallback) {
  const rawValue = __ENV[name];
  if (rawValue === undefined || rawValue === '') {
    return fallback;
  }

  const value = Number(rawValue);
  if (!Number.isInteger(value) || value <= 0) {
    throw new Error(`${name} must be a positive integer. Received: ${rawValue}`);
  }

  return value;
}

export const workloads = Object.freeze({
  load: {
    targetVus: positiveInteger('LOAD_TARGET_VUS', 10),
    rampUp: __ENV.LOAD_RAMP_UP || '30s',
    steady: __ENV.LOAD_STEADY_DURATION || '2m',
    rampDown: __ENV.LOAD_RAMP_DOWN || '30s',
  },
  stress: {
    firstTargetVus: positiveInteger('STRESS_FIRST_TARGET_VUS', 20),
    secondTargetVus: positiveInteger('STRESS_SECOND_TARGET_VUS', 50),
    finalTargetVus: positiveInteger('STRESS_FINAL_TARGET_VUS', 100),
    stageDuration: __ENV.STRESS_STAGE_DURATION || '1m',
  },
  soak: {
    vus: positiveInteger('SOAK_VUS', 10),
    duration: __ENV.SOAK_DURATION || '30m',
  },
});
