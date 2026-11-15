import { expect, test } from '@playwright/test';
import { apps } from '../apps.ts';

for (const app of apps) {
  test(`the ${app.name} app loads`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error') {
        errors.push(message.text());
      }
    });

    await page.goto(app.url);

    await expect(page).toHaveTitle('HireLab');
    await expect(page.getByRole('banner')).toContainText('HireLab');
    expect(errors).toEqual([]);
  });
}
