import { expect, test } from '@playwright/test';
import { apps } from '../apps.ts';

// The staff app asks the API who is signed in; for an anonymous visitor the browser logs that 401.
function isAnonymousSessionCheck(text: string): boolean {
  return text.includes('status of 401');
}

for (const app of apps) {
  test(`the ${app.name} app loads`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error' && !isAnonymousSessionCheck(message.text())) {
        errors.push(message.text());
      }
    });

    await page.goto(app.url);

    await expect(page).toHaveTitle('HireLab');
    await expect(page.getByRole('banner')).toContainText('HireLab');
    expect(errors).toEqual([]);
  });
}
