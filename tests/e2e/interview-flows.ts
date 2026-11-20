import { test as base, type BrowserContext, expect, type Page } from '@playwright/test';
import { candidateApp } from './apps.ts';
import { logIn, openPage } from './staff-flows.ts';
import { seededUsers } from './staff-users.ts';

const [, seededManager] = seededUsers;

/** Opens a candidate link in a browser context of its own, as the candidate, without the staff cookie. */
export const test = base.extend<{ openAsCandidate: (link: string) => Promise<Page> }>({
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

/** Logs in as the seeded manager, creates a position with a candidate and plans their first session. */
export async function planSession(
  page: Page,
  title: string,
  fullName: string,
  email: string,
): Promise<void> {
  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'Job positions');
  await page.getByRole('link', { name: 'New position' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Requirements').fill('- Explains fake services');
  await page.getByRole('button', { name: 'Create position' }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();

  await page.getByRole('link', { name: 'Add candidate' }).click();
  await page.getByLabel('Full name').fill(fullName);
  await page.getByLabel('Email').fill(email);
  await page.getByRole('button', { name: 'Add candidate' }).click();
  await expect(page.getByRole('heading', { name: fullName })).toBeVisible();

  await page.getByRole('button', { name: 'Plan session' }).click();
  await expect(page.getByRole('heading', { name: `Session 1 · ${fullName}` })).toBeVisible();
}

/** Adds a task from the bank to the session's queue by its title. */
export async function queueTask(page: Page, taskTitle: string): Promise<void> {
  await page.getByRole('button', { name: 'Add to queue' }).click();
  await page.getByRole('button', { name: `Add ${taskTitle} to the queue` }).click();
  await expect(page.getByRole('dialog').getByText('Queued')).toBeVisible();
  await page.getByRole('button', { name: 'Done' }).click();
  await expect(page.getByRole('list', { name: 'Task queue' })).toContainText(taskTitle);
}

/** Creates the candidate link and returns it on the candidate app, which the dev setup serves on its own port. */
export async function createCandidateLink(page: Page): Promise<string> {
  await page.getByRole('button', { name: 'Create candidate link' }).click();
  await page.getByRole('button', { name: 'Create link', exact: true }).click();
  const link = page.getByRole('textbox', { name: 'Candidate link' });
  await expect(link).toHaveValue(/\/s\/.+/);
  const { pathname } = new URL(await link.inputValue());
  await page.getByRole('button', { name: 'Done' }).click();
  return new URL(pathname, candidateApp.url).toString();
}
