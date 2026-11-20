import { expect } from '@playwright/test';
import { candidateApp } from '../apps.ts';
import { createCandidateLink, planSession, queueTask, test } from '../interview-flows.ts';

const seededTask = 'Explain dependency injection';
const answer = 'Constructor injection keeps the **dependencies** of a fake service explicit.';

test.describe.configure({ timeout: 180_000 });

test('an interviewer runs a whole interview with an open question and the candidate answers live', async ({
  openAsCandidate,
  page,
}) => {
  const fullName = 'Jane Fakeinterview';
  await planSession(
    page,
    `E2E Fake Interview Role ${Date.now()}`,
    fullName,
    'jane.fakeinterview@example.com',
  );
  await queueTask(page, seededTask);
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
