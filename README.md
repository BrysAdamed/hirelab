# HireLab

A platform for live technical interviews. Interviewers queue tasks from a bank (code review, working with an AI assistant, live coding), send the candidate a link, and watch the results come in while the call is running. Afterwards they score each answer against a rubric and a reference answer, so their impressions don't fade after the fifth interview of the day.

> Status: early design. There is no code yet. See [docs/vision.md](docs/vision.md).

## Why

A single `.cs` file sent before a screen share doesn't test much now that AI agents write and review code. It doesn't show whether a candidate pastes confidential data into an LLM, and it leaves nothing behind once the call ends. HireLab replaces it with a queue of tasks the interviewer can adjust during the call, each with its own UI, and a saved record of what the candidate did.

## Docs

- [Vision](docs/vision.md)
- [Glossary](docs/glossary.md)
- [Architecture decisions](docs/adr/README.md)
- [Design system](docs/design-system.md)
- [Operations](docs/operations.md)

## Stack

.NET 10 with Aspire (modular monolith), Angular with Spartan UI and Tailwind, PostgreSQL, SignalR, Azure Container Apps in the EU. The reasoning is in the ADRs.

## License

[MIT](LICENSE)
