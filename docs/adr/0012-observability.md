# 0012. Observability

## Context

We need enough traces, logs and metrics to debug a failed interview, without a monitoring bill and without spreading personal data. HireLab handles candidates' names, emails, CVs and responses, and logs are the usual place where such data leaks.

## Decision

**OpenTelemetry through Aspire ServiceDefaults** for traces, metrics and logs in every project.

| Where | Telemetry goes to |
|---|---|
| Local | The Aspire dashboard |
| Azure | Workspace-based Application Insights |

In Azure, sampling is on and Log Analytics has a daily cap of 0.2 GB.

**No personal data in telemetry.**
- Never logged or traced: candidate names, emails, attachment names, response content, prompts, tokens, cookies.
- Use ids instead.
- Request bodies and query strings are never logged.
- Log messages use structured templates with ids, not interpolated objects.

**Alerts.** None besides the budget alert ([0010](0010-hosting-and-environments.md)). The single user notices outages directly.

## Consequences

- Telemetry costs are capped. On a busy day the cap can drop telemetry, which is acceptable for this project.
- Debugging relies on correlating ids with the database, never on reading personal data from logs.
- Anonymisation doesn't have to touch telemetry, because telemetry holds no personal data.
- Without alerts, problems surface only when someone uses the app.

## Status

Accepted
