import { expect, test, type Page } from '@playwright/test';
import { candidateApp, staffApp, type App } from '../apps.ts';

// Both apps ask the API who is signed in (the candidate app reads its interview); for an anonymous visitor the browser
// logs that 401.
function isAnonymousSessionCheck(text: string): boolean {
  return text.includes('status of 401');
}

interface Landing {
  app: App;
  expectLanded: (page: Page) => Promise<void>;
}

const landings: Landing[] = [
  {
    app: staffApp,
    expectLanded: async (page) => {
      await expect(page).toHaveURL(/\/login\?returnUrl=%2F$/);
      await expect(page).toHaveTitle('Log in - HireLab');
      await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
    },
  },
  {
    app: candidateApp,
    expectLanded: async (page) => {
      await expect(page).toHaveTitle('HireLab');
    },
  },
];

for (const { app, expectLanded } of landings) {
  test(`the ${app.name} app loads`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error' && !isAnonymousSessionCheck(message.text())) {
        errors.push(message.text());
      }
    });

    await page.goto(app.url);

    await expectLanded(page);
    await expect(page.getByRole('banner')).toContainText('HireLab');
    expect(errors).toEqual([]);
  });
}
