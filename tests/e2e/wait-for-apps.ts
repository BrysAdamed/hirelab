import { apps, type App } from './apps.ts';

const timeoutMs = 5 * 60_000;
const pollIntervalMs = 2_000;

async function isUp(app: App): Promise<boolean> {
  try {
    const response = await fetch(app.url);
    return response.ok;
  } catch {
    return false;
  }
}

async function waitFor(app: App, deadline: number): Promise<void> {
  while (!(await isUp(app))) {
    if (Date.now() > deadline) {
      throw new Error(`The ${app.name} app didn't start at ${app.url} within ${timeoutMs / 1000} s.`);
    }
    await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
  }
}

export default async function waitForApps(): Promise<void> {
  const deadline = Date.now() + timeoutMs;
  await Promise.all(apps.map((app) => waitFor(app, deadline)));
}
