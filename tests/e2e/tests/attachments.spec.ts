import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { expect, test } from '@playwright/test';
import { logIn, logOut, openPage } from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const [, seededManager, seededInterviewer] = seededUsers;
const fakeCv = fileURLToPath(new URL('../fixtures/fake-cv.pdf', import.meta.url));

test.describe.configure({ timeout: 120_000 });

test('a manager creates a position, an interviewer adds a candidate, uploads a PDF and downloads it', async ({
  page,
}) => {
  const title = `E2E Fake Analyst ${Date.now()}`;

  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- Reads fake reports');
  await page.getByRole('button', { name: 'Create position' }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();
  await logOut(page, seededManager.displayName);

  await logIn(page, seededInterviewer.email, seededInterviewer.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: title }).click();
  await page.getByRole('link', { name: 'Add candidate' }).click();
  await page.getByLabel('Full name').fill('John Fakecandidate');
  await page.getByRole('button', { name: 'Add candidate' }).click();
  await expect(page.getByRole('heading', { name: 'John Fakecandidate' })).toBeVisible();
  await expect(page.getByText('No attachments yet.')).toBeVisible();

  const chooser = page.waitForEvent('filechooser');
  await page.getByRole('button', { name: 'Upload file' }).click();
  await (await chooser).setFiles(fakeCv);

  await expect(page.getByRole('cell', { name: 'fake-cv.pdf', exact: true })).toBeVisible();
  await expect(page.getByText(`by ${seededInterviewer.displayName}`)).toBeVisible();
  await expect(page.getByText(/1 of 10 files/)).toBeVisible();

  const download = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Download fake-cv.pdf' }).click();
  const downloaded = await download;
  expect(downloaded.suggestedFilename()).toBe('fake-cv.pdf');
  expect(readFileSync(await downloaded.path())).toEqual(readFileSync(fakeCv));
});
