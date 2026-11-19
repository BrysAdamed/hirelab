# 0032. Code review task type

## Context

Epic HLB-7 builds the first real task type on the plug-in contract of [0028](0028-task-type-plug-in-contract.md): a pull-request-like review with a diff, inline comments and a summary. Its first task is a port of the original DonationRequest CQRS review. The refinement of HLB-7 (2026-11-19) settled the model, the diff technology and what the server checks. This record keeps those decisions in one place for the stories under HLB-7.

## Decision

### Key and models

The task type key is `code-review`, with content schema version 1 and response schema version 1.

| Part | Holds |
|---|---|
| Candidate content | `title`, `description` (Markdown, at most 20,000 characters) and 1–20 **changed files**: `path` (at most 300 characters, unique), `language`, optional `original` and `modified` (each at most 200 KB). An added file has no `original`. |
| Staff content | At most 50 **expected findings**: `id` (kebab-case, unique), `file`, `side` (`original` / `modified`), `startLine`, `endLine`, `title`, `description` (Markdown) and `severity` (`Critical`, `Major`, `Minor`, `Nit`). |
| Response | At most 200 **review comments**: `id` (a client-generated `Guid`, so whole-document autosave can edit and delete one), `file`, `side`, `startLine`, `endLine`, `body` (Markdown, 1–5,000 characters); a `summary` (Markdown, at most 20,000 characters) and an optional `verdict` (`Approve`, `RequestChanges`, `Comment`). |

`language` is a closed list the type validates: `csharp`, `typescript`, `javascript`, `html`, `css`, `json`, `sql`, `xml`, `yaml`, `markdown`, `plaintext`. Each value maps to a Monaco grammar, so a new language is a code change.

`ValidateContent` also checks that every expected finding points at an existing file and side and at lines inside it, since it sees both content parts.

### Review comment anchors are not validated on the server

`ValidateResponse` receives only the response ([0028](0028-task-type-plug-in-contract.md)), so the server checks a comment's structure (side, `1 ≤ startLine ≤ endLine`, body length, limits) but not that its file and lines exist in the task. We considered passing the content to `ValidateResponse`, which changes the contract of every type, and chose not to:

- the candidate UI only creates comments on lines it shows;
- a `Current` task's pinned content never changes under the candidate ([0031](0031-interview-sessions-and-responses.md));
- only a candidate tampering with their own requests can store a bad anchor, and it harms nobody but their own response.

The review UI therefore lists a comment whose file or lines don't exist as **not anchored**, apart from the diff, instead of failing.

### No score suggestions

`SuggestScores` returns nothing. Matching review comments to expected findings by overlapping lines is noisy (a right comment one line off, a wrong comment on the right line), and a suggestion the interviewer can't trust is worse than none. The review UI shows expected findings next to the comments and the interviewer judges. Suggestions can come back once Evaluation (HLB-8) shows them.

### Monaco, self-hosted and lazy, in the `ui` lib

The diff and the editors use Monaco from the `monaco-editor` npm package, self-hosted with its workers (no CDN, as the project is self-contained). We considered the CodeMirror 6 merge view (much lighter) and our own diff render. Monaco won because the vision asks for a task UI that resembles the tools people use, and live coding (HLB-10) needs a full editor anyway.

The wrappers (`hl-code-diff`, `hl-code-editor`) live in a secondary entry point `@hirelab/ui/code`, our code like the rest of the `ui` lib ([0016](0016-ui-stack.md)). Their theme is built from the design tokens in light and dark. Only the code-review entry points import it, lazily, so Monaco never reaches the initial bundle of either app.

### Frontend entry points

- **candidate**: the PR header, a file list with comment counts, one file's diff at a time (inline by default, side by side on a toggle), comments added from a gutter button, a "Comment" button for the current line or selection, or the Monaco action `Ctrl+Alt+M`, shown under their lines; a "Finish review" panel with the summary and verdict.
- **review**: the same diff with the comments read-only, the summary and verdict, a "Not anchored" list, and the expected findings as a second layer (markers in the diff and a side list) under "Show expected findings", off while `live` and on afterwards. No manual ticking: scoring belongs to Evaluation.
- **editor**: the full content model (PR header, add, remove and rename files, both sides in Monaco, expected findings with typed file, side and lines). The staff page that hosts editors comes with HLB-22.

## Consequences

- A code review task is content only: a new review is authored in the app, with no code change, as long as its languages are on the list.
- The server can store a review comment on a line that doesn't exist; the review UI has to handle it, and does by listing it apart.
- Monaco adds several MB, paid only by pages that show a code-review task, and is shared with live coding.
- Interviewers compare comments with expected findings by eye until Evaluation adds suggestions.

## Status

Accepted
