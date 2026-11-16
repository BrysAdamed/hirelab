/** The key of the open question task type, the backend's `OpenQuestionTaskType.TaskTypeKey`. */
export const openQuestionKey = 'open-question';

/** The most characters an answer may have, as the backend validates it. */
export const answerMaxLength = 20_000;

/** The most characters a prompt may have, as the backend validates it. */
export const promptMaxLength = 20_000;

/** What the candidate sees: the question, in Markdown. */
export interface OpenQuestionContent {
  readonly prompt: string;
}

/** An open question has no staff-only content. */
export type OpenQuestionStaffContent = Readonly<Record<string, never>>;

/** The candidate's answer, in Markdown. */
export interface OpenQuestionResponse {
  readonly answer: string;
}
