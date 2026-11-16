import { Routes } from '@angular/router';
import { InterviewPage } from './interview/interview-page';
import { openLink } from './interview/open-link';

export const routes: Routes = [
  { path: '', component: InterviewPage },
  { path: ':token', canActivate: [openLink], children: [] },
  { path: '**', redirectTo: '' },
];
