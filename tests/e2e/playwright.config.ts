import { defineConfig, devices } from '@playwright/test';
import { staffApp } from './apps.ts';

const isCi = !!process.env['CI'];

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: isCi,
  retries: isCi ? 1 : 0,
  reporter: isCi ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  globalSetup: './wait-for-apps.ts',
  expect: { timeout: 15_000 },
  use: {
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../../src/HireLab.AppHost --launch-profile http',
    url: staffApp.url,
    reuseExistingServer: !isCi && process.env['E2E_REUSE_APP'] === '1',
    timeout: 10 * 60_000,
    gracefulShutdown: { signal: 'SIGINT', timeout: 30_000 },
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      ASPIRE_ALLOW_UNSECURED_TRANSPORT: 'true',
      HIRELAB_E2E_AUTH_PERMIT_LIMIT: '1000',
      HIRELAB_E2E_EPHEMERAL_DATA: 'true',
    },
  },
});
