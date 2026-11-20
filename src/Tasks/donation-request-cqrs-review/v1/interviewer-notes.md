Give the candidate a minute to look through the files before talking. Let them review in their own order; the interesting part is what they notice first and how they phrase it. Comments such as "this is wrong" with no reason score lower on communication than a question or a concrete suggestion.

## Hints

Use these only if the candidate is stuck or has missed a whole area.

- "Who is the user here?" (points at `UserLogin` in the command body and the missing authorisation)
- "What does `new Guid()` return?"
- "What happens if someone calls the process endpoint twice?"
- "What does the client get back when the request doesn't exist?"

## Follow-up questions

- How would you make processing idempotent? What should the second call return?
- Where should `UserLogin` come from instead, and how does the handler get it?
- Why `decimal` rather than `double` for money? Would you go further, for example a `Money` type?
- What test would you write first for this change? (A good answer: processing an already processed request fails and leaves the first decision untouched.)
- Who should be allowed to process a request, and where would you enforce it?

## What to watch for

- Mid level: the identity and `new Guid()` problems should be found without hints; most of the Major items too.
- The check `spots-identity-from-body` is critical: answer **yes** only if the candidate spots it without the first hint.
- Extra findings that are reasonable but not on the list (logging, naming, the commands returning nothing useful to the client) count towards the scale criteria.
