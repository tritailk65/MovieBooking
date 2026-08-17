import { check, fail, sleep } from 'k6';

export function pollUntil({
  name,
  timeoutSeconds,
  intervalSeconds,
  request,
  predicate,
  describeLastResult = () => '<no result description>',
}) {
  const deadline = Date.now() + timeoutSeconds * 1000;
  let lastResult = null;

  while (Date.now() < deadline) {
    lastResult = request();

    if (predicate(lastResult)) {
      check(true, {
        [`${name} completed within ${timeoutSeconds}s`]: (value) => value,
      });
      return lastResult;
    }

    sleep(intervalSeconds);
  }

  check(false, {
    [`${name} completed within ${timeoutSeconds}s`]: (value) => value,
  });

  fail(
    `${name} timed out after ${timeoutSeconds}s. ` +
      `Last result: ${describeLastResult(lastResult)}`,
  );

  return lastResult;
}
