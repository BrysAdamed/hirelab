import { expect, test, type Page } from '@playwright/test';
import { staffApp } from '../apps.ts';
import { seededUsers } from '../staff-users.ts';

const inviteePassword = 'invitee-password-not-a-secret';

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

async function fillInvitee(page: Page, email: string, displayName: string): Promise<void> {
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Display name').fill(displayName);
}

async function issuedLink(page: Page): Promise<string> {
  await page.getByRole('button', { name: 'Create invite' }).click();
  const link = page.getByLabel('Invite link');
  await expect(link).toHaveValue(/\/invite#.+/);
  return link.inputValue();
}

async function redeem(page: Page, link: string, expectedInvite: RegExp): Promise<void> {
  await page.goto(link);
  await expect(page.getByText(expectedInvite)).toBeVisible();
  await page.getByLabel('Password', { exact: true }).fill(inviteePassword);
  await page.getByLabel('Confirm password').fill(inviteePassword);
  await page.getByRole('button', { name: 'Create account' }).click();
}

test('the admin invites a manager, who invites an interviewer, who logs in', async ({ page }) => {
  const [admin] = seededUsers;
  const run = Date.now();
  const teamName = `E2E Team ${run}`;
  const manager = { email: `e2e.manager.${run}@example.com`, displayName: `Mia Manager ${run}` };
  const interviewer = {
    email: `e2e.interviewer.${run}@example.com`,
    displayName: `Ike Interviewer ${run}`,
  };

  await logIn(page, admin.email, admin.password);
  await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Teams' }).click();
  await fillInvitee(page, manager.email, manager.displayName);
  await page.getByLabel('Team name').fill(teamName);
  const managerLink = await issuedLink(page);
  await expect(page.getByRole('row', { name: new RegExp(manager.email) })).toContainText('Pending');
  await logOut(page, admin.displayName);

  await redeem(page, managerLink, new RegExp(`manager of a new team, ${teamName}`));
  await expect(
    page.getByRole('heading', { name: `Welcome, ${manager.displayName}` }),
  ).toBeVisible();

  await page
    .getByRole('navigation', { name: 'Main' })
    .getByRole('link', { name: 'My team' })
    .click();
  await expect(page.getByText(teamName)).toBeVisible();
  await fillInvitee(page, interviewer.email, interviewer.displayName);
  const interviewerLink = await issuedLink(page);
  await logOut(page, manager.displayName);

  await redeem(page, interviewerLink, new RegExp(`an interviewer in ${teamName}`));
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
  await logOut(page, interviewer.displayName);

  await logIn(page, interviewer.email, inviteePassword);
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
  await expect(page.getByText(`You are signed in as interviewer in ${teamName}.`)).toBeVisible();

  await page.goto(interviewerLink);
  await expect(page.getByRole('alert')).toContainText('This invite link has already been used.');
});
