import http from 'k6/http';
import { check, sleep } from 'k6';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5280';

export const options = {
  scenarios: {
    smoke_load: {
      executor: 'constant-vus',
      vus: 10,
      duration: '30s'
    }
  },
  thresholds: {
    'http_req_failed': ['rate<0.01'],           // Error rate < 1%
    'http_req_duration': ['p(95)<2000', 'p(99)<3000'],  // p95 < 2s, p99 < 3s
    'http_reqs': ['rate>10']                    // At least 10 req/sec throughput
  }
};

// Read-only endpoints to hit under load
const endpoints = [
  '/health',
  '/api/models?inStockOnly=true&limit=50',
  '/api/models?search=bosch&limit=20',
  '/api/customers?limit=50',
  '/api/exchangerates/USD/UAH',
  '/api/exchangerates/EUR/UAH',
  '/api/reports/stock',
  '/api/invoices?limit=20'
];

export default function() {
  // Randomly select an endpoint
  const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
  const res = http.get(`${BASE_URL}${endpoint}`);

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time is acceptable': (r) => r.timings.duration < 2000,
    'response has body': (r) => r.body && r.body.length > 0
  });

  // Small think time between requests (100ms)
  sleep(0.1);
}

export function handleSummary(data) {
  return {
    'tests/e2e/results/load-summary.json': JSON.stringify(data, null, 2),
    'tests/e2e/results/load-junit.xml': generateJUnitXML(data),
    'stdout': textSummary(data, { indent: ' ', enableColors: true })
  };
}

function generateJUnitXML(data) {
  const timestamp = new Date().toISOString();
  const totalDuration = data.state.testRunDurationMs / 1000;

  // For load tests, we check metrics against thresholds
  const metrics = data.metrics;
  const thresholds = Object.entries(options.thresholds);

  let totalTests = 0;
  let totalFailures = 0;

  const testCases = [];

  // Check each threshold
  thresholds.forEach(([metricName, thresholdDefs]) => {
    const metric = metrics[metricName];
    if (!metric) return;

    thresholdDefs.forEach(thresholdDef => {
      totalTests++;
      const testName = `${metricName} ${thresholdDef}`;

      // Check if threshold passed
      const thresholdResult = metric.thresholds && metric.thresholds[thresholdDef];
      const passed = thresholdResult ? thresholdResult.ok : false;

      if (!passed) {
        totalFailures++;
        testCases.push(`      <testcase name="${escapeXml(testName)}" classname="LoadTest.Thresholds">
        <failure message="Threshold not met" type="threshold">
          Metric: ${metricName}
          Threshold: ${thresholdDef}
          Value: ${JSON.stringify(metric.values)}
        </failure>
      </testcase>`);
      } else {
        testCases.push(`      <testcase name="${escapeXml(testName)}" classname="LoadTest.Thresholds" />`);
      }
    });
  });

  // Add checks as test cases
  if (data.root_group && data.root_group.checks) {
    Object.entries(data.root_group.checks).forEach(([checkName, checkData]) => {
      totalTests++;
      if (checkData.fails > 0) {
        totalFailures++;
        testCases.push(`      <testcase name="${escapeXml(checkName)}" classname="LoadTest.Checks">
        <failure message="Check failed ${checkData.fails} time(s)" type="check">
          Passes: ${checkData.passes}
          Fails: ${checkData.fails}
        </failure>
      </testcase>`);
      } else {
        testCases.push(`      <testcase name="${escapeXml(checkName)}" classname="LoadTest.Checks" />`);
      }
    });
  }

  return `<?xml version="1.0" encoding="UTF-8"?>
<testsuites name="HotWind E2E Load Tests" tests="${totalTests}" failures="${totalFailures}" time="${totalDuration}">
    <testsuite name="Load Test Results" tests="${totalTests}" failures="${totalFailures}" timestamp="${timestamp}">
${testCases.join('\n')}
    </testsuite>
</testsuites>`;
}

function escapeXml(unsafe) {
  return unsafe.replace(/[<>&'"]/g, (c) => {
    switch (c) {
      case '<': return '&lt;';
      case '>': return '&gt;';
      case '&': return '&amp;';
      case '\'': return '&apos;';
      case '"': return '&quot;';
      default: return c;
    }
  });
}
