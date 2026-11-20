import { expect, type Locator, type Page } from '@playwright/test';
import { createCandidateLink, planSession, queueTask, test } from '../interview-flows.ts';

const seededTask = 'Review a donation request pull request';
const commandFile = 'src/Application/CreateRequestCommand.cs';
const controllerFile = 'src/Api/Controllers/DonationRequestController.cs';

test.describe.configure({ timeout: 240_000 });

function changedFile(page: Page, path: string): Locator {
  return page
    .getByRole('navigation', { name: 'Changed files' })
    .getByRole('button', { name: new RegExp(`^${path.replaceAll('.', '\\.')} `) });
}

function codeLine(page: Page, text: string): Locator {
  return page.locator('.monaco-editor .view-line', { hasText: text });
}

test('a candidate reviews the donation request pull request and the interviewer sees the comments live', async ({
  openAsCandidate,
  page,
}) => {
  const fullName = 'Jane Fakereviewer';
  await planSession(
    page,
    `E2E Fake Review Role ${Date.now()}`,
    fullName,
    'jane.fakereviewer@example.com',
  );
  await queueTask(page, seededTask);
  const link = await createCandidateLink(page);

  const candidate = await openAsCandidate(link);
  await expect(page.getByText(/Candidate online/)).toBeVisible();
  await page.getByRole('button', { name: 'Go live' }).click();

  await expect(candidate.getByRole('heading', { name: 'Add donation request endpoints' })).toBeVisible();
  await expect(candidate.getByText('If anything is unclear, ask.')).toBeVisible();
  await changedFile(candidate, commandFile).click();
  await expect(candidate.getByRole('region', { name: `Review of ${commandFile}` })).toBeVisible();

  await codeLine(candidate, 'public string UserLogin').click();
  await candidate.getByRole('button', { name: 'Comment', exact: true }).click();
  const commentBox = candidate.getByRole('textbox', { name: 'Comment on line 5 (modified)' });
  await expect(commentBox).toBeFocused();
  await candidate.keyboard.type('Who is the user? Take it from the **authenticated** principal.');
  await candidate.getByRole('button', { name: 'Add comment' }).click();
  await expect(candidate.getByRole('article', { name: 'Comment on line 5 (modified)' })).toContainText(
    'Take it from the authenticated principal.',
  );
  await expect(changedFile(candidate, commandFile)).toContainText('1');

  await changedFile(candidate, controllerFile).click();
  await codeLine(candidate, 'return Ok(id);').hover();
  await candidate.locator('.hl-code-gutter-button').click();
  await candidate.getByRole('textbox', { name: 'Comment on line 19 (modified)' }).fill('Return 201 Created here.');
  await candidate.getByRole('button', { name: 'Add comment' }).click();
  await expect(candidate.getByRole('article', { name: 'Comment on line 19 (modified)' })).toBeVisible();

  await candidate.getByRole('textbox', { name: 'Review summary' }).fill('Blocking: the user comes from the body.');
  await candidate.getByRole('radio', { name: 'Request changes' }).check();
  await expect(candidate.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();

  await expect(page.getByRole('button', { name: 'Show expected findings' })).toHaveAttribute(
    'aria-pressed',
    'false',
  );
  await expect(changedFile(page, commandFile)).toContainText('1 comment');
  await expect(page.getByText('Blocking: the user comes from the body.')).toBeVisible();
  await expect(page.getByText('Verdict: Request changes')).toBeVisible();
  await changedFile(page, commandFile).click();
  await expect(page.getByRole('article', { name: 'Comment on line 5 (modified)' })).toContainText(
    'Take it from the authenticated principal.',
  );
  await expect(page.getByRole('heading', { name: /Not anchored/ })).toBeHidden();

  await page.getByRole('button', { name: `Mark ${seededTask} done` }).click();
  await page.getByRole('button', { name: 'Finish', exact: true }).click();
  await page.getByRole('button', { name: 'Finish session' }).click();
  await expect(candidate.getByRole('heading', { name: 'This interview has ended' })).toBeVisible();

  await expect(page.getByRole('button', { name: 'Show expected findings' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await expect(page.getByRole('heading', { name: /Expected findings \(\d+\)/ })).toBeVisible();
  await page.getByRole('button', { name: /new Guid\(\) is Guid\.Empty/ }).click();
  await expect(
    page.getByRole('region', { name: 'Review of src/Application/CreateRequestCommandHandler.cs' }),
  ).toBeVisible();
  await expect(codeLine(page, 'donationRequest.Id = new Guid();')).toBeVisible();
});
