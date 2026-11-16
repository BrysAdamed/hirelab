import { test as base, type BrowserContext, expect, type Page } from '@playwright/test';
import { candidateApp } from '../apps.ts';
import { logIn, openPage } from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const [, seededManager] = seededUsers;
const seededTask = 'Explain dependency injection';
const answer = 'Constructor injection keeps the **dependencies** of a fake service explicit.';

/** Opens a candidate link in a browser context of its own, as the candidate, without the staff cookie. */
const test = base.extend<{ openAsCandidate: (link: string) => Promise<Page> }>({
  openAsCandidate: async ({ browser }, use) => {
    const contexts: BrowserContext[] = [];
    await use(async (link) => {
      const context = await browser.newContext();
      contexts.push(context);
      const page = await context.newPage();
      await page.goto(link);
      return page;
    });
    await Promise.all(contexts.map((context) => context.close()));
  },
});

test.describe.configure({ timeout: 180_000 });

async function planSession(page: Page, title: string, fullName: string): Promise<void> {
  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- Explains fake services');
  await page.getByRole('button', { name: 'Create position' }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();

  await page.getByRole('link', { name: 'Add candidate' }).click();
  await page.getByLabel('Full name').fill(fullName);
  await page.getByLabel('Email').fill('jane.fakeinterview@example.com');
  await page.getByRole('button', { name: 'Add candidate' }).click();
  await expect(page.getByRole('heading', { name: fullName })).toBeVisible();

  await page.getByRole('button', { name: 'Plan session' }).click();
  await expect(page.getByRole('heading', { name: `Session 1 · ${fullName}` })).toBeVisible();
}

async function queueSeededTask(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Add to queue' }).click();
  await page.getByRole('button', { name: `Add ${seededTask} to the queue` }).click();
  await expect(page.getByRole('dialog').getByText('Queued')).toBeVisible();
  await page.getByRole('button', { name: 'Done' }).click();
  await expect(page.getByRole('list', { name: 'Task queue' })).toContainText(seededTask);
}

/** Creates the candidate link and returns it on the candidate app, which the dev setup serves on its own port. */
async function createCandidateLink(page: Page): Promise<string> {
  await page.getByRole('button', { name: 'Create candidate link' }).click();
  await page.getByRole('button', { name: 'Create link', exact: true }).click();
  const link = page.getByRole('textbox', { name: 'Candidate link' });
  await expect(link).toHaveValue(/\/s\/.+/);
  const { pathname } = new URL(await link.inputValue());
  await page.getByRole('button', { name: 'Done' }).click();
  return new URL(pathname, candidateApp.url).toString();
}

test('an interviewer runs a whole interview with an open question and the candidate answers live', async ({
  openAsCandidate,
  page,
}) => {
  const fullName = 'Jane Fakeinterview';
  await planSession(page, `E2E Fake Interview Role ${Date.now()}`, fullName);
  await queueSeededTask(page);
  const link = await createCandidateLink(page);
  await expect(page.getByText('Candidate not connected yet')).toBeVisible();

  const candidate = await openAsCandidate(link);
  await expect(candidate).toHaveURL(candidateApp.url);
  await expect(candidate.getByRole('heading', { name: 'Your interview hasn’t started yet' })).toBeVisible();
  await expect(page.getByText(/Candidate online/)).toBeVisible();

  await page.getByRole('button', { name: 'Go live' }).click();
  const answerBox = candidate.getByRole('textbox', { name: 'Your answer' });
  await expect(answerBox).toBeVisible();
  await answerBox.fill(answer);
  await expect(candidate.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();

  await expect(page.getByRole('heading', { name: 'Question', exact: true })).toBeVisible();
  const answerSection = page.getByRole('heading', { name: 'Answer', exact: true }).locator('..');
  await expect(answerSection).toContainText('Constructor injection keeps the dependencies');
  await expect(answerSection.getByText('dependencies', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: `Mark ${seededTask} done` }).click();
  await expect(candidate.getByRole('heading', { name: 'That was the last task' })).toBeVisible();

  await page.getByRole('button', { name: 'Finish', exact: true }).click();
  await page.getByRole('button', { name: 'Finish session' }).click();
  await expect(page.getByText('The candidate link stopped working when the session finished.')).toBeVisible();
  await expect(candidate.getByRole('heading', { name: 'This interview has ended' })).toBeVisible();

  const reopened = await openAsCandidate(link);
  await expect(reopened.getByRole('heading', { name: 'This interview has ended' })).toBeVisible();
  await expect(reopened.getByRole('textbox', { name: 'Your answer' })).toBeHidden();
});
