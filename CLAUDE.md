# HireLab - notes for agents

Live technical interview platform: a coded task bank, interview sessions with a task queue, and live results for interviewers. Before starting a ticket, read `docs/vision.md` and `docs/glossary.md`. Before changing anything structural, check `docs/adr/`.

## Hard rules

- **Public repo.** Never commit real personal data: candidate names, CVs, emails, company names, real interview notes. Use invented examples. Secrets go in user secrets or environment variables only.
- Planted "personal data" and "secrets" in AI-assistant tasks must be obviously fake (reserved domains such as `example.com`, test card numbers, made-up names).
- Use glossary terms in code, API and UI. When you introduce a new term, add it to `docs/glossary.md`.
- Every structural decision gets a new ADR in `docs/adr/` (next number) and a row in `docs/adr/README.md`.
- Tasks are code. A change to a task's content bumps its version; never edit a version that sessions have already used.
- The UI is in English only.
- No PrimeNG (paid licence). The component library is chosen in HLB-1.

## Work tracking

Tickets live in the ticketing app (MCP server `ticketing`), project key `HLB`. Epics are HLB-1 to HLB-11. Branch names and commit messages start with the ticket key, e.g. `HLB-12 Add job position aggregate`.

## Layout and commands

The repo has no code yet. The stack is .NET (with Aspire) and Angular, on Azure in the EU; the details come from the HLB-1 ADRs and the HLB-2 walking skeleton. Once the skeleton exists, update this section with the layout and the build, run and test commands.
