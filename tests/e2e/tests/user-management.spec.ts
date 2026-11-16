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

const [admin, seededManager] = seededUsers;

interface Person {
  email: string;
  displayName: string;
}

/** The seeded manager invites an interviewer into their team, who redeems the link and logs out. */
async function addInterviewerToSeededTeam(page: Page, interviewer: Person): Promise<void> {
  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'My team');
  await fillInvitee(page, interviewer.email, interviewer.displayName);
  const link = await issuedLink(page);
  await logOut(page, seededManager.displayName);

  await redeem(page, link, /an interviewer in/);
  await expect(
    page.getByRole('heading', { name: `Welcome, ${interviewer.displayName}` }),
  ).toBeVisible();
  await logOut(page, interviewer.displayName);
}

test('the admin reassigns an interviewer to another team', async ({ page }) => {
  const run = Date.now();
  const interviewer = {
    email: `e2e.reassigned.${run}@example.com`,
    displayName: `Rea Reassigned ${run}`,
  };
  const targetTeam = `E2E Target Team ${run}`;
  const targetManager = {
    email: `e2e.target.manager.${run}@example.com`,
    displayName: `Tom Target ${run}`,
  };

  await addInterviewerToSeededTeam(page, interviewer);

  await logIn(page, admin.email, admin.password);
  await openPage(page, 'Teams');
  await fillInvitee(page, targetManager.email, targetManager.displayName);
  await page.getByLabel('Team name').fill(targetTeam);
  const managerLink = await issuedLink(page);
  await logOut(page, admin.displayName);
  await redeem(page, managerLink, new RegExp(`manager of a new team, ${targetTeam}`));
  await expect(
    page.getByRole('heading', { name: `Welcome, ${targetManager.displayName}` }),
  ).toBeVisible();
  await logOut(page, targetManager.displayName);

  await logIn(page, admin.email, admin.password);
  await openPage(page, 'Teams');
  await page.getByRole('button', { name: `Reassign ${interviewer.displayName}` }).click();
  await page.getByLabel('New team').selectOption({ label: targetTeam });
  await page.getByRole('button', { name: 'Move to team' }).click();
  await expect(
    page.getByRole('region', { name: targetTeam }).getByRole('row', { name: interviewer.email }),
  ).toBeVisible();
  await logOut(page, admin.displayName);

  await logIn(page, interviewer.email, inviteePassword);
  await expect(
    page.getByText(`You are signed in as interviewer in ${targetTeam}.`),
  ).toBeVisible();
});

test('a manager deactivates an interviewer, who then cannot log in', async ({ page }) => {
  const run = Date.now();
  const interviewer = {
    email: `e2e.deactivated.${run}@example.com`,
    displayName: `Dee Deactivated ${run}`,
  };

  await addInterviewerToSeededTeam(page, interviewer);

  await logIn(page, seededManager.email, seededManager.password);
  await openPage(page, 'My team');
  await page.getByRole('button', { name: `Deactivate ${interviewer.displayName}` }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Deactivate user' }).click();
  await expect(page.getByRole('row', { name: interviewer.email })).toContainText('Deactivated');
  await logOut(page, seededManager.displayName);

  await logIn(page, interviewer.email, inviteePassword);
  await expect(page.getByRole('alert')).toContainText('The email or password is incorrect');
  await expect(page).toHaveURL(/\/login$/);
});
