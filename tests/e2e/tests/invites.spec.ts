import { expect, test } from '@playwright/test';
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

test.describe.configure({ timeout: 120_000 });

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
  await openPage(page, 'Teams');
  await fillInvitee(page, manager.email, manager.displayName);
  await page.getByLabel('Team name').fill(teamName);
  const managerLink = await issuedLink(page);
  await expect(page.getByRole('row', { name: new RegExp(manager.email) })).toContainText('Pending');
  await logOut(page, admin.displayName);

  await redeem(page, managerLink, new RegExp(`manager of a new team, ${teamName}`));
  await expect(
    page.getByRole('heading', { name: `Welcome, ${manager.displayName}` }),
  ).toBeVisible();

  await openPage(page, 'My team');
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
