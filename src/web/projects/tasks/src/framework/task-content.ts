/** A task version's content: the part the candidate sees and the staff-only part (the backend's `TaskContent`). */
export interface TaskContent<TCandidateContent, TStaffContent> {
  readonly candidate: TCandidateContent;
  readonly staff: TStaffContent;
}
