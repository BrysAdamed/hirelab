import { readFileSync } from 'node:fs';

interface SeedPerson {
  email: string;
  displayName: string;
}

interface DevelopmentSeed {
  password: string;
  admin: SeedPerson;
  teams: { name: string; manager: SeedPerson; interviewers: SeedPerson[] }[];
}

export interface StaffUser extends SeedPerson {
  role: 'Admin' | 'Manager' | 'Interviewer';
  password: string;
}

const seed = JSON.parse(
  readFileSync(
    new URL(
      '../../src/Modules/Identity/HireLab.Modules.Identity/Seed/development-seed.json',
      import.meta.url,
    ),
    'utf8',
  ),
) as DevelopmentSeed;

const [sampleTeam] = seed.teams;

/** The users the development seed creates, one per role, with the seed's shared fake password. */
export const seededUsers: readonly StaffUser[] = [
  { role: 'Admin', ...seed.admin, password: seed.password },
  { role: 'Manager', ...sampleTeam.manager, password: seed.password },
  { role: 'Interviewer', ...sampleTeam.interviewers[0], password: seed.password },
];
