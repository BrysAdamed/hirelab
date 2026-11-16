# 0023. Candidate personal data stays in Positions

## Context

A candidate's name, email, phone and profile links are the only personal data HireLab keeps about people outside the company. They are needed in several places: the position's candidates table, the candidate profile, interview sessions (HLB-6) and evaluations and decisions (HLB-8). Closing a position, or anonymising one candidate, must remove that data everywhere and show the candidate as "Candidate #N" while notes, scores and task results stay (HLB-43).

If each module copied the name it shows, anonymisation would have to find and rewrite every copy, in every module's schema, and a copy that is missed would keep personal data after the candidate was promised it was gone.

## Decision

**One owner.** Candidate personal data lives only in the Positions module (`positions.candidates`). No other module stores a candidate's name, email, phone, links or attachments, not even as a denormalised display copy, a cached value or a snapshot.

**Other modules hold the id.** Sessions, Evaluation and later modules store only the `CandidateId` (`HireLab.Modules.Positions.Contracts.CandidateId`) and read what they show through `ICandidateDirectory` in `Positions.Contracts`: a `CandidateSummary` with the id, the job position id, the display name and an anonymised flag, by one id or a set of ids. They read it again whenever they show it.

**One display rule.** The display name is the full name, or "Candidate #N" once the candidate is anonymised, where N is the candidate's number in its position. The rule lives on the `Candidate` aggregate (`Candidate.DisplayName`); endpoints and the directory use it, never their own formatting.

**Team-scoped like any read.** The directory goes through the Positions DbContext and its team query filter ([0009](0009-authorisation-model.md)), so it sees only the current staff user's team. A candidate's own requests ([0008](0008-candidate-access.md)) and code outside a request find no candidates through it; a system job that needs names gets its own explicit, tested path.

**Anonymisation happens in one place.** Anonymising a candidate clears the contact details in Positions, sets `AnonymisedBy`/`AnonymisedAt` and deletes the attachments; nothing else has to change for every screen to show "Candidate #N".

## Consequences

- Anonymisation is one update in one schema, and is complete by construction.
- Every screen that shows candidates costs a call to the directory. `FindManyAsync` resolves a whole list in one query, so a list never needs one call per row.
- Logs and traces carry candidate ids only ([0012](0012-observability.md)), which this decision already requires of other modules.
- A module that needs a new piece of candidate data asks for it to be added to `Positions.Contracts`, which keeps what leaves Positions visible and reviewable.
- The rule is listed in `CLAUDE.md`, since a copied name would not fail any test until anonymisation.

## Status

Accepted
