import { expect, test, type Page } from '@playwright/test';
import {
  fillInvitee,
  inviteePassword,
  issuedLink,
  logIn,
  logOut,
  openPage,
  redeem,
} from '../staff-flows.ts';
import { seededUsers } from '../staff-users.ts';

const resetPassword = 'reset-password-not-a-secret';
const changedPassword = 'changed-password-not-a-secret';

const manager = seededUsers.find((user) => user.role === 'Manager')!;

/** The seeded manager invites a new interviewer, who sets their first password and logs out. */
async function inviteInterviewer(
  page: Page,
  interviewer: { email: string; displayName: string },
): Promise<void> {
  await logIn(page, manager.email, manager.password);
  await openPage(page, 'My team');
  await fillInvitee(page, interviewer.email, interviewer.displayName);
  const link = await issuedLink(page);
  await logOut(page, manager.displayName);

  await redeem(page, link, /an interviewer in/);
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
  await logOut(page, interviewer.displayName);
}

test('a manager resets an interviewer’s password and the interviewer logs in with the new one', async ({
  page,
}) => {
  const run = Date.now();
  const interviewer = { email: `e2e.reset.${run}@example.com`, displayName: `Rita Reset ${run}` };
  await inviteInterviewer(page, interviewer);

  await logIn(page, manager.email, manager.password);
  await openPage(page, 'My team');
  await page
    .getByRole('button', { name: `Reset the password of ${interviewer.displayName}` })
    .click();
  await page.getByRole('button', { name: 'Generate link' }).click();
  const resetLink = page.getByLabel('Reset link');
  await expect(resetLink).toHaveValue(/\/reset#.+/);
  const link = await resetLink.inputValue();
  await page.getByRole('button', { name: 'Close' }).click();
  await logOut(page, manager.displayName);

  await page.goto(link);
  await expect(page.getByText(`${interviewer.displayName}, choose a new password`)).toBeVisible();
  await page.getByLabel('New password').fill(resetPassword);
  await page.getByLabel('Confirm new password').fill(resetPassword);
  await page.getByRole('button', { name: 'Set password' }).click();
  await expect(page.getByRole('status')).toContainText('Your password is changed');

  await logIn(page, interviewer.email, inviteePassword);
  await expect(page.getByRole('alert')).toBeVisible();
  await logIn(page, interviewer.email, resetPassword);
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();

  await page.goto(link);
  await expect(page.getByRole('alert')).toContainText('This reset link has already been used.');
});

test('a user changes their own password from the user menu', async ({ page }) => {
  const run = Date.now();
  const interviewer = { email: `e2e.change.${run}@example.com`, displayName: `Carl Change ${run}` };
  await inviteInterviewer(page, interviewer);

  await logIn(page, interviewer.email, inviteePassword);
  await page.getByRole('button', { name: interviewer.displayName }).click();
  await page.getByRole('menuitem', { name: 'Change password' }).click();
  await page.getByLabel('Current password').fill(inviteePassword);
  await page.getByLabel('New password', { exact: true }).fill(changedPassword);
  await page.getByLabel('Confirm new password').fill(changedPassword);
  await page.getByRole('button', { name: 'Change password' }).click();
  await expect(page.getByRole('status')).toContainText('Your password is changed.');
  await logOut(page, interviewer.displayName);

  await logIn(page, interviewer.email, changedPassword);
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
});
