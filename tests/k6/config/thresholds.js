export const gatewaySmokeThresholds = {
  checks: ['rate==1'],
  http_req_failed: ['rate==0'],
  http_req_duration: ['p(95)<2000'],
};

export const happyPathSmokeThresholds = {
  checks: ['rate==1'],
  'http_req_failed{phase:business}': ['rate==0'],
  'http_req_duration{phase:business}': ['p(95)<2000'],
};

export const loadThresholds = {
  checks: ['rate>0.99'],
  'http_req_failed{phase:business}': ['rate<0.01'],
  'http_req_duration{phase:business}': [
    'p(95)<1000',
    'p(99)<2000',
  ],
};

export const stressThresholds = {
  checks: ['rate>0.95'],
  'http_req_failed{phase:business}': ['rate<0.05'],
  'http_req_duration{phase:business}': ['p(95)<2000'],
};

export const soakThresholds = {
  checks: ['rate>0.99'],
  'http_req_failed{phase:business}': ['rate<0.01'],
  'http_req_duration{phase:business}': ['p(95)<1000'],
};
