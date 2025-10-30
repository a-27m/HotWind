import http from 'k6/http';
import { check, group } from 'k6';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5280';

export const options = {
  scenarios: {
    functional: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 1
    }
  },
  thresholds: {
    'http_req_failed': ['rate<0.01'],
    'http_req_duration': ['p(95)<2000']
  }
};

// Test data - will be populated during test execution
let testData = {
  validCustomerId: null,
  validSku: null,
  createdInvoiceId: null
};

export default function() {
  // 01 - Health Check
  group('01 - Health Check', () => {
    const res = http.get(`${BASE_URL}/health`);
    check(res, {
      'health returns 200': (r) => r.status === 200,
      'health has status field': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.status === 'healthy';
        } catch (e) {
          return false;
        }
      },
      'health has timestamp': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.timestamp !== undefined;
        } catch (e) {
          return false;
        }
      }
    });
  });

  // 02 - Models API
  group('02 - Models API', () => {
    // GET /api/models - list all
    const listRes = http.get(`${BASE_URL}/api/models?limit=100`);
    check(listRes, {
      'models list returns 200': (r) => r.status === 200,
      'models list has success true': (r) => JSON.parse(r.body).success === true,
      'models list has data array': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data) && body.data.length > 0;
      },
      'model has required fields': (r) => {
        const body = JSON.parse(r.body);
        const model = body.data[0];
        return model.sku && model.modelName && model.manufacturer && model.capacityKw !== undefined;
      }
    });

    // Store a valid SKU for later tests
    if (listRes.status === 200) {
      const body = JSON.parse(listRes.body);
      if (body.data && body.data.length > 0) {
        testData.validSku = body.data[0].sku;
      }
    }

    // GET /api/models?search=bosch - search
    const searchRes = http.get(`${BASE_URL}/api/models?search=bosch`);
    check(searchRes, {
      'models search returns 200': (r) => r.status === 200,
      'search results contain bosch': (r) => {
        const body = JSON.parse(r.body);
        return body.data.length > 0 &&
               body.data.some(m => m.modelName.toLowerCase().includes('bosch'));
      }
    });

    // GET /api/models?inStockOnly=true - filter
    const stockRes = http.get(`${BASE_URL}/api/models?inStockOnly=true`);
    check(stockRes, {
      'in-stock filter returns 200': (r) => r.status === 200,
      'in-stock models have stock': (r) => {
        const body = JSON.parse(r.body);
        return body.data.every(m => m.stockLevel > 0);
      }
    });

    // GET /api/models/{sku} - get by SKU
    if (testData.validSku) {
      const getRes = http.get(`${BASE_URL}/api/models/${testData.validSku}`);
      check(getRes, {
        'get model by SKU returns 200': (r) => r.status === 200,
        'model SKU matches': (r) => {
          const body = JSON.parse(r.body);
          return body.data.sku === testData.validSku;
        }
      });
    }

    // GET /api/models/INVALID-SKU - 404 test
    const notFoundRes = http.get(`${BASE_URL}/api/models/INVALID-NONEXISTENT-SKU-999`);
    check(notFoundRes, {
      'invalid SKU returns 404': (r) => r.status === 404,
      'error has success false': (r) => JSON.parse(r.body).success === false
    });
  });

  // 03 - Customers API
  group('03 - Customers API', () => {
    // GET /api/customers - list all
    const listRes = http.get(`${BASE_URL}/api/customers?limit=100`);
    check(listRes, {
      'customers list returns 200': (r) => r.status === 200,
      'customers list has data array': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data) && body.data.length > 0;
      },
      'customer has required fields': (r) => {
        const body = JSON.parse(r.body);
        const customer = body.data[0];
        return customer.customerId && customer.companyName && customer.email;
      }
    });

    // Store a valid customer ID for later tests
    if (listRes.status === 200) {
      const body = JSON.parse(listRes.body);
      if (body.data && body.data.length > 0) {
        testData.validCustomerId = body.data[0].customerId;
      }
    }

    // GET /api/customers?search=industrial - search
    const searchRes = http.get(`${BASE_URL}/api/customers?search=industrial`);
    check(searchRes, {
      'customers search returns 200': (r) => r.status === 200,
      'search returns array': (r) => Array.isArray(JSON.parse(r.body).data)
    });

    // GET /api/customers/{id} - get by ID
    if (testData.validCustomerId) {
      const getRes = http.get(`${BASE_URL}/api/customers/${testData.validCustomerId}`);
      check(getRes, {
        'get customer by ID returns 200': (r) => r.status === 200,
        'customer ID matches': (r) => {
          const body = JSON.parse(r.body);
          return body.data.customerId === testData.validCustomerId;
        }
      });
    }

    // GET /api/customers/999999 - 404 test
    const notFoundRes = http.get(`${BASE_URL}/api/customers/999999`);
    check(notFoundRes, {
      'invalid customer ID returns 404': (r) => r.status === 404,
      'error has success false': (r) => JSON.parse(r.body).success === false
    });
  });

  // 04 - Exchange Rates
  group('04 - Exchange Rates', () => {
    // GET /api/exchangerates/USD/UAH - get rate
    const rateRes = http.get(`${BASE_URL}/api/exchangerates/USD/UAH`);
    check(rateRes, {
      'get exchange rate returns 200': (r) => r.status === 200,
      'rate is a positive number': (r) => {
        const body = JSON.parse(r.body);
        return typeof body.data === 'number' && body.data > 0;
      }
    });

    // GET /api/exchangerates/USD/UAH?date=2024-06-15 - historical
    const historicalRes = http.get(`${BASE_URL}/api/exchangerates/USD/UAH?date=2024-06-15`);
    check(historicalRes, {
      'historical rate returns 200': (r) => r.status === 200,
      'historical rate is valid': (r) => {
        const body = JSON.parse(r.body);
        return typeof body.data === 'number' && body.data > 0;
      }
    });

    // GET /api/exchangerates/EUR/EUR - same currency (should be 1.0)
    const sameCurrencyRes = http.get(`${BASE_URL}/api/exchangerates/EUR/EUR`);
    check(sameCurrencyRes, {
      'same currency returns 200': (r) => r.status === 200,
      'same currency rate is 1.0': (r) => {
        const body = JSON.parse(r.body);
        return body.data === 1.0;
      }
    });

    // POST /api/exchangerates/generate - generate rates
    const generatePayload = JSON.stringify({
      startDate: '2025-01-01',
      endDate: '2025-01-07'
    });

    const generateRes = http.post(
      `${BASE_URL}/api/exchangerates/generate`,
      generatePayload,
      { headers: { 'Content-Type': 'application/json' } }
    );

    check(generateRes, {
      'generate rates returns 200': (r) => r.status === 200,
      'generated count is positive': (r) => {
        const body = JSON.parse(r.body);
        return typeof body.data === 'number' && body.data >= 0;
      }
    });

    // GET /api/exchangerates/XXX/YYY - 404 test (invalid currencies)
    const invalidRes = http.get(`${BASE_URL}/api/exchangerates/XXX/YYY?date=2024-01-01`);
    check(invalidRes, {
      'invalid currency pair returns 404 or 400': (r) => r.status === 404 || r.status === 400
    });

    // POST /api/exchangerates/generate - invalid date range (end before start)
    const invalidPayload = JSON.stringify({
      startDate: '2025-12-31',
      endDate: '2025-01-01'
    });

    const invalidGenerateRes = http.post(
      `${BASE_URL}/api/exchangerates/generate`,
      invalidPayload,
      { headers: { 'Content-Type': 'application/json' } }
    );

    check(invalidGenerateRes, {
      'invalid date range returns 400': (r) => r.status === 400
    });
  });

  // 05 - Invoices (FIFO Logic)
  group('05 - Invoices API', () => {
    // First, get valid customer and in-stock SKU from earlier tests
    if (!testData.validCustomerId || !testData.validSku) {
      console.log('Warning: Missing test data for invoice creation');
      return;
    }

    // POST /api/invoices - create invoice
    const invoicePayload = JSON.stringify({
      customerId: testData.validCustomerId,
      invoiceDate: '2024-12-15',
      lines: [
        {
          sku: testData.validSku,
          quantity: 1,
          unitPrice: 25000.00
        }
      ]
    });

    const createRes = http.post(
      `${BASE_URL}/api/invoices`,
      invoicePayload,
      { headers: { 'Content-Type': 'application/json' } }
    );

    check(createRes, {
      'create invoice returns 200': (r) => r.status === 200,
      'created invoice has ID': (r) => {
        const body = JSON.parse(r.body);
        return body.data && body.data.invoiceId > 0;
      },
      'invoice has line items': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data.lines) && body.data.lines.length > 0;
      }
    });

    // Store created invoice ID for retrieval test
    if (createRes.status === 200) {
      const body = JSON.parse(createRes.body);
      if (body.data && body.data.invoiceId) {
        testData.createdInvoiceId = body.data.invoiceId;
      }
    }

    // GET /api/invoices/{id} - get created invoice
    if (testData.createdInvoiceId) {
      const getRes = http.get(`${BASE_URL}/api/invoices/${testData.createdInvoiceId}`);
      check(getRes, {
        'get invoice returns 200': (r) => r.status === 200,
        'invoice ID matches': (r) => {
          const body = JSON.parse(r.body);
          return body.data.invoiceId === testData.createdInvoiceId;
        }
      });
    }

    // GET /api/invoices?limit=10 - list recent
    const listRes = http.get(`${BASE_URL}/api/invoices?limit=10`);
    check(listRes, {
      'list invoices returns 200': (r) => r.status === 200,
      'invoice list is array': (r) => Array.isArray(JSON.parse(r.body).data)
    });

    // POST /api/invoices - invalid customer (should return 400)
    const invalidCustomerPayload = JSON.stringify({
      customerId: 999999,
      invoiceDate: '2024-12-15',
      lines: [
        {
          sku: testData.validSku,
          quantity: 1,
          unitPrice: 25000.00
        }
      ]
    });

    const invalidCustomerRes = http.post(
      `${BASE_URL}/api/invoices`,
      invalidCustomerPayload,
      { headers: { 'Content-Type': 'application/json' } }
    );

    check(invalidCustomerRes, {
      'invalid customer returns 400': (r) => r.status === 400,
      'error message present': (r) => {
        const body = JSON.parse(r.body);
        return body.success === false && body.error;
      }
    });

    // POST /api/invoices - invalid SKU (should return 400)
    const invalidSkuPayload = JSON.stringify({
      customerId: testData.validCustomerId,
      invoiceDate: '2024-12-15',
      lines: [
        {
          sku: 'INVALID-SKU-999',
          quantity: 1,
          unitPrice: 25000.00
        }
      ]
    });

    const invalidSkuRes = http.post(
      `${BASE_URL}/api/invoices`,
      invalidSkuPayload,
      { headers: { 'Content-Type': 'application/json' } }
    );

    check(invalidSkuRes, {
      'invalid SKU returns 400': (r) => r.status === 400
    });

    // GET /api/invoices/999999 - 404 test
    const notFoundRes = http.get(`${BASE_URL}/api/invoices/999999`);
    check(notFoundRes, {
      'nonexistent invoice returns 404': (r) => r.status === 404
    });
  });

  // 06 - Reports (Complex SQL)
  group('06 - Reports API', () => {
    // GET /api/reports/stock - stock report
    const stockRes = http.get(`${BASE_URL}/api/reports/stock`);
    check(stockRes, {
      'stock report returns 200': (r) => r.status === 200,
      'stock report has data': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data) && body.data.length > 0;
      },
      'stock items have required fields': (r) => {
        const body = JSON.parse(r.body);
        const item = body.data[0];
        return item.sku && item.modelName && item.stockLevel !== undefined;
      }
    });

    // GET /api/reports/price-list - price list report
    const priceListRes = http.get(`${BASE_URL}/api/reports/price-list`);
    check(priceListRes, {
      'price list returns 200': (r) => r.status === 200,
      'price list has data': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data) && body.data.length > 0;
      },
      'price items have pricing fields': (r) => {
        const body = JSON.parse(r.body);
        const item = body.data[0];
        return item.weightedLotValueUah !== undefined && item.currentMarketValueUah !== undefined;
      }
    });

    // GET /api/reports/currency-translation?from=2024-01-01&to=2024-12-31 - currency report
    const currencyRes = http.get(`${BASE_URL}/api/reports/currency-translation?from=2024-01-01&to=2024-12-31`);
    check(currencyRes, {
      'currency report returns 200': (r) => r.status === 200,
      'currency report has data': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.data);
      }
    });

    // GET /api/reports/currency-translation?from=2025-12-31&to=2025-01-01 - 400 test (invalid range)
    const invalidRangeRes = http.get(`${BASE_URL}/api/reports/currency-translation?from=2025-12-31&to=2025-01-01`);
    check(invalidRangeRes, {
      'invalid date range returns 400': (r) => r.status === 400,
      'error indicates invalid range': (r) => {
        const body = JSON.parse(r.body);
        return body.success === false;
      }
    });

    // GET /api/reports/currency-translation (missing parameters) - 400/422 test
    const missingParamsRes = http.get(`${BASE_URL}/api/reports/currency-translation`);
    check(missingParamsRes, {
      'missing date params returns 400': (r) => r.status === 400
    });
  });

  // 07 - Error Handling Summary
  group('07 - HTTP Status Code Verification', () => {
    // Verify we're not getting 5xx errors unexpectedly
    const healthRes = http.get(`${BASE_URL}/health`);
    check(healthRes, {
      'no unexpected 500 errors': (r) => r.status !== 500 && r.status !== 503
    });

    // Test that 4xx errors are properly structured
    const notFoundRes = http.get(`${BASE_URL}/api/models/NONEXISTENT`);
    check(notFoundRes, {
      '404 has proper structure': (r) => {
        if (r.status !== 404) return false;
        const body = JSON.parse(r.body);
        return body.success === false && body.error;
      }
    });

    // Test that validation errors return 400
    const badRequestRes = http.post(
      `${BASE_URL}/api/invoices`,
      JSON.stringify({ invalidField: 'test' }),
      { headers: { 'Content-Type': 'application/json' } }
    );
    check(badRequestRes, {
      '400 for bad request': (r) => r.status === 400 || r.status === 422,
      '400 has error message': (r) => {
        const body = JSON.parse(r.body);
        return body.success === false;
      }
    });
  });
}

export function handleSummary(data) {
  return {
    'tests/e2e/results/functional-summary.json': JSON.stringify(data, null, 2),
    'tests/e2e/results/functional-junit.xml': generateJUnitXML(data),
    'stdout': textSummary(data, { indent: ' ', enableColors: true })
  };
}

function generateJUnitXML(data) {
  const timestamp = new Date().toISOString();
  const totalDuration = data.state.testRunDurationMs / 1000;

  let totalTests = 0;
  let totalFailures = 0;

  const testSuites = Object.entries(data.root_group.groups || {}).map(([groupName, group]) => {
    const checks = Object.entries(group.checks || {});
    const failures = checks.filter(([_, c]) => c.fails > 0);

    totalTests += checks.length;
    totalFailures += failures.length;

    const testCases = checks.map(([checkName, checkData]) => {
      if (checkData.fails > 0) {
        return `      <testcase name="${escapeXml(checkName)}" classname="${escapeXml(groupName)}">
        <failure message="Check failed ${checkData.fails} time(s)" type="assertion">
          Passes: ${checkData.passes}
          Fails: ${checkData.fails}
        </failure>
      </testcase>`;
      }
      return `      <testcase name="${escapeXml(checkName)}" classname="${escapeXml(groupName)}" />`;
    }).join('\n');

    return `    <testsuite name="${escapeXml(groupName)}" tests="${checks.length}" failures="${failures.length}" timestamp="${timestamp}">
${testCases}
    </testsuite>`;
  }).join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<testsuites name="HotWind E2E Functional Tests" tests="${totalTests}" failures="${totalFailures}" time="${totalDuration}">
${testSuites}
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
