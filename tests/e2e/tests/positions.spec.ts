import { expect, test } from '@playwright/test';
import { logIn, openPage } from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const [, seededManager] = seededUsers;

test.describe.configure({ timeout: 120_000 });

test('a manager creates, edits and deletes a job position', async ({ page }) => {
  const title = `E2E Fake Developer ${Date.now()}`;

  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- **Three** years of fake work');
  await page.getByRole('button', { name: 'Create position' }).click();

  await expect(page.getByRole('heading', { name: title })).toBeVisible();
  await expect(page.getByText('Three', { exact: true })).toBeVisible();

  await page.getByRole('link', { name: 'Edit' }).click();
  await page.getByLabel('Title').fill(`${title} Senior`);
  await page.getByRole('button', { name: 'Save changes' }).click();
  await expect(page.getByRole('heading', { name: `${title} Senior` })).toBeVisible();

  await openPage(page, 'Job positions');
  await expect(page.getByRole('link', { name: `${title} Senior` })).toBeVisible();
  await page.getByRole('link', { name: `${title} Senior` }).click();
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.getByRole('button', { name: 'Delete position' }).click();

  await expect(page).toHaveURL(/\/positions$/);
  await expect(page.getByRole('link', { name: `${title} Senior` })).toHaveCount(0);
});
