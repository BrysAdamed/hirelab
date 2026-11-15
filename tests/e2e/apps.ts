export interface App {
  name: string;
  url: string;
}

export const staffApp: App = { name: 'staff', url: 'http://localhost:4200/' };
export const candidateApp: App = { name: 'candidate', url: 'http://localhost:4201/s/' };

export const apps: readonly App[] = [staffApp, candidateApp];
