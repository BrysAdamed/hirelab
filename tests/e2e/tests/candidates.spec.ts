import { expect, test } from '@playwright/test';
import { logIn, openPage } from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const [, seededManager] = seededUsers;

test.describe.configure({ timeout: 120_000 });

test('a manager adds a candidate to a position and edits their details', async ({ page }) => {
  const title = `E2E Fake Tester ${Date.now()}`;

  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- Tests fake apps');
  await page.getByRole('button', { name: 'Create position' }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();

  await page.getByRole('link', { name: 'Add candidate' }).click();
  await page.getByLabel('Full name').fill('Jane Fakeperson');
  await page.getByLabel(/Email/).fill('jane.fakeperson@example.com');
  await page.getByLabel('Profile link 1').fill('https://profiles.example.com/jane-fakeperson');
  await page.getByRole('button', { name: 'Add candidate' }).click();

  await expect(page.getByRole('heading', { name: 'Jane Fakeperson' })).toBeVisible();
  await expect(page.getByText(/Candidate #1/)).toBeVisible();

  await page.getByRole('link', { name: 'Edit' }).click();
  await page.getByLabel('Full name').fill('Jane Q. Fakeperson');
  await page.getByRole('button', { name: 'Save changes' }).click();
  await expect(page.getByRole('heading', { name: 'Jane Q. Fakeperson' })).toBeVisible();

  await page.getByRole('link', { name: title }).click();
  await expect(page.getByRole('link', { name: 'Jane Q. Fakeperson' })).toBeVisible();
});
