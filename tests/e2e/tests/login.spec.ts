import { expect, test } from '@playwright/test';
import { staffApp } from '../apps.ts';
import { seededUsers, type StaffUser } from '../staff-users.ts';

const navigationByRole: Record<StaffUser['role'], string[]> = {
  Admin: ['Home', 'Teams', 'Users'],
  Manager: ['Home', 'Job positions', 'Sessions', 'Task bank', 'My team'],
  Interviewer: ['Home', 'Job positions', 'Sessions', 'Task bank'],
};

for (const user of seededUsers) {
  test(`the ${user.role.toLowerCase()} logs in, sees their navigation and logs out`, async ({
    page,
  }) => {
    await page.goto(staffApp.url);
    await expect(page).toHaveURL(/\/login\?returnUrl=%2F$/);

    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password').fill(user.password);
    await page.getByRole('button', { name: 'Log in' }).click();

    await expect(page.getByRole('heading', { name: `Welcome, ${user.displayName}` })).toBeVisible();
    await expect(
      page.getByRole('navigation', { name: 'Main' }).getByRole('link'),
    ).toHaveText(navigationByRole[user.role]);

    await page.getByRole('button', { name: user.displayName }).click();
    await page.getByRole('menuitem', { name: 'Log out' }).click();

    await expect(page).toHaveURL(/\/login$/);
    await page.goto(staffApp.url);
    await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
  });
}

test('a wrong password shows an error and keeps the user on the login page', async ({ page }) => {
  await page.goto(new URL('login', staffApp.url).toString());

  await page.getByLabel('Email').fill('nobody@example.com');
  await page.getByLabel('Password').fill('wrong-password-not-a-secret');
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page.getByRole('alert')).toContainText('The email or password is incorrect');
  await expect(page).toHaveURL(/\/login$/);
});

test('after login the user returns to the page they asked for', async ({ page }) => {
  const [, manager] = seededUsers;
  await page.goto(new URL('tasks', staffApp.url).toString());
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Ftasks$/);

  await page.getByLabel('Email').fill(manager.email);
  await page.getByLabel('Password').fill(manager.password);
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page).toHaveURL(/\/tasks$/);
  await expect(page.getByRole('heading', { name: 'Task bank' })).toBeVisible();
});
