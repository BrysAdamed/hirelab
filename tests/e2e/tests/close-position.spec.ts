import { fileURLToPath } from 'node:url';
import { expect, type Page, test } from '@playwright/test';
import { logIn, openPage } from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const [, seededManager] = seededUsers;
const fakeCv = fileURLToPath(new URL('../fixtures/fake-cv.pdf', import.meta.url));

test.describe.configure({ timeout: 120_000 });

async function createPositionWithCandidate(page: Page, title: string, fullName: string): Promise<void> {
  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- Closes fake positions');
  await page.getByRole('button', { name: 'Create position' }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();

  await page.getByRole('link', { name: 'Add candidate' }).click();
  await page.getByLabel('Full name').fill(fullName);
  await page.getByLabel('Email').fill('jane.fakeclosed@example.com');
  await page.getByRole('button', { name: 'Add candidate' }).click();
  await expect(page.getByRole('heading', { name: fullName })).toBeVisible();

  const chooser = page.waitForEvent('filechooser');
  await page.getByRole('button', { name: 'Upload file' }).click();
  await (await chooser).setFiles(fakeCv);
  await expect(page.getByRole('cell', { name: 'fake-cv.pdf', exact: true })).toBeVisible();
}

test('a manager closes the position, the candidate shows as Candidate #1 and their attachments are gone', async ({
  page,
}) => {
  const title = `E2E Fake Closed Role ${Date.now()}`;
  await createPositionWithCandidate(page, title, 'Jane Fakeclosed');

  await page.getByRole('link', { name: title }).click();
  await page.getByRole('button', { name: 'Close position' }).click();
  await expect(page.getByRole('dialog')).toContainText('attachments are deleted');
  await page.getByRole('button', { name: 'Close and anonymise' }).click();

  await expect(page.getByText(`by ${seededManager.displayName}. The position is read-only.`)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Close position' })).toBeHidden();
  await page.getByRole('link', { name: 'Candidate #1' }).click();

  await expect(page.getByRole('heading', { name: 'Candidate #1' })).toBeVisible();
  await expect(page.getByText(/Anonymised on .* by /)).toBeVisible();
  await expect(page.getByText('jane.fakeclosed@example.com')).toBeHidden();
  await expect(page.getByText('No attachments yet.')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Edit' })).toBeHidden();
});

test('a manager anonymises one candidate and the position stays open', async ({ page }) => {
  const title = `E2E Fake Withdrawn Role ${Date.now()}`;
  await createPositionWithCandidate(page, title, 'John Fakewithdrawn');

  await page.getByRole('button', { name: 'Anonymise' }).click();
  await expect(page.getByRole('dialog')).toContainText('Candidate #1');
  await page.getByRole('button', { name: 'Anonymise candidate' }).click();

  await expect(page.getByRole('heading', { name: 'Candidate #1' })).toBeVisible();
  await expect(page.getByText(`by ${seededManager.displayName}. Their contact details`)).toBeVisible();
  await expect(page.getByText('No attachments yet.')).toBeVisible();

  await page.getByRole('link', { name: title }).click();
  await expect(page.getByRole('button', { name: 'Close position' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Add candidate' })).toBeVisible();
});
