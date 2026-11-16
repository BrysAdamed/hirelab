import { expect, type Page } from '@playwright/test';
import { staffApp } from './apps.ts';

/** The password every invitee in the suite sets. */
export const inviteePassword = 'invitee-password-not-a-secret';

export async function logIn(page: Page, email: string, password: string): Promise<void> {
  await page.goto(new URL('login', staffApp.url).toString());
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();
}

export async function logOut(page: Page, displayName: string): Promise<void> {
  await page.getByRole('button', { name: displayName }).click();
  await page.getByRole('menuitem', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/login$/);
}

export async function openPage(page: Page, label: string): Promise<void> {
  await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: label }).click();
}

export async function fillInvitee(page: Page, email: string, displayName: string): Promise<void> {
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Display name').fill(displayName);
}

/** Creates the invite filled in on the page and returns its link. */
export async function issuedLink(page: Page): Promise<string> {
  await page.getByRole('button', { name: 'Create invite' }).click();
  const link = page.getByLabel('Invite link');
  await expect(link).toHaveValue(/\/invite#.+/);
  return link.inputValue();
}

/** Redeems an invite link with {@link inviteePassword}; the invitee ends up signed in. */
export async function redeem(page: Page, link: string, expectedInvite: RegExp): Promise<void> {
  await page.goto(link);
  await expect(page.getByText(expectedInvite)).toBeVisible();
  await page.getByLabel('Password', { exact: true }).fill(inviteePassword);
  await page.getByLabel('Confirm password').fill(inviteePassword);
  await page.getByRole('button', { name: 'Create account' }).click();
}
