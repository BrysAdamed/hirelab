import { expect, test, type Page } from '@playwright/test';
import { staffApp } from '../apps.ts';
import { seededUsers } from '../staff-users.ts';

const firstPassword = 'first-password-not-a-secret';
const resetPassword = 'reset-password-not-a-secret';
const changedPassword = 'changed-password-not-a-secret';

async function logIn(page: Page, email: string, password: string): Promise<void> {
  await page.goto(new URL('login', staffApp.url).toString());
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();
}

async function logOut(page: Page, displayName: string): Promise<void> {
  await page.getByRole('button', { name: displayName }).click();
  await page.getByRole('menuitem', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/login$/);
}

async function openMyTeam(page: Page): Promise<void> {
  await page
    .getByRole('navigation', { name: 'Main' })
    .getByRole('link', { name: 'My team' })
    .click();
}

/** The seeded manager invites a new interviewer, who sets a first password and logs out. */
async function inviteInterviewer(
  page: Page,
  interviewer: { email: string; displayName: string },
): Promise<void> {
  const manager = seededUsers.find((user) => user.role === 'Manager')!;
  await logIn(page, manager.email, manager.password);
  await openMyTeam(page);
  await page.getByLabel('Email', { exact: true }).fill(interviewer.email);
  await page.getByLabel('Display name').fill(interviewer.displayName);
  await page.getByRole('button', { name: 'Create invite' }).click();
  const link = await page.getByLabel('Invite link').inputValue();
  await logOut(page, manager.displayName);

  await page.goto(link);
  await page.getByLabel('Password', { exact: true }).fill(firstPassword);
  await page.getByLabel('Confirm password').fill(firstPassword);
  await page.getByRole('button', { name: 'Create account' }).click();
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
  await logOut(page, interviewer.displayName);
}

test('a manager resets an interviewer’s password and the interviewer logs in with the new one', async ({
  page,
}) => {
  const manager = seededUsers.find((user) => user.role === 'Manager')!;
  const run = Date.now();
  const interviewer = {
    email: `e2e.reset.${run}@example.com`,
    displayName: `Rita Reset ${run}`,
  };
  await inviteInterviewer(page, interviewer);

  await logIn(page, manager.email, manager.password);
  await openMyTeam(page);
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
  await page.getByRole('link', { name: 'Log in' }).click();

  await logIn(page, interviewer.email, firstPassword);
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
  const interviewer = {
    email: `e2e.change.${run}@example.com`,
    displayName: `Carl Change ${run}`,
  };
  await inviteInterviewer(page, interviewer);

  await logIn(page, interviewer.email, firstPassword);
  await page.getByRole('button', { name: interviewer.displayName }).click();
  await page.getByRole('menuitem', { name: 'Change password' }).click();
  await page.getByLabel('Current password').fill(firstPassword);
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
