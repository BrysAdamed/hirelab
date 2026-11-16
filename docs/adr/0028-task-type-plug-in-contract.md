# 0028. Task type plug-in contract

## Context

[0013](0013-task-module-contract.md) sketched the backend contract of a task type and said the signatures weren't final. [0015](0015-task-authoring-in-the-app.md) moved tasks into the database and added `ValidateContent` and the `editor` entry point. Epic HLB-5 builds the framework before the first real task type (HLB-7), so the contract, its home and the storage decisions that depend on it have to be fixed now. The refinement of HLB-5 (2026-11-16) settled them; this record keeps them in one place.

This ADR supersedes the **Backend contract** section of 0013. The rest of 0013 and 0015 stands, except where noted below.

## Decision

### The contract lives in its own project

`src/TaskTypes/HireLab.TaskTypes.Abstractions` holds the contract, the registry, the endpoint mapping and the rubric and suggestion models. It references only `HireLab.SharedKernel` (for `Result`) and ASP.NET Core (for endpoint mapping), never a module.

- Each task type is a project `src/TaskTypes/HireLab.TaskTypes.<Name>` that references the Abstractions and, at most, module `.Contracts` projects.
- Modules (TaskBank, Sessions, Evaluation) reference the Abstractions, never a type project.
- The API host references the type projects and registers them in `TaskTypeCatalog`.
- Architecture tests enforce the references.

### Signatures

```csharp
public interface ITaskType
{
    string Key { get; }                  // kebab-case, for example "code-review"
    Type CandidateContentType { get; }
    Type StaffContentType { get; }
    Type ResponseType { get; }
    int ContentSchemaVersion { get; }
    int ResponseSchemaVersion { get; }

    Result ValidateContent(JsonElement candidateContent, JsonElement staffContent);
    Result ValidateResponse(JsonElement response);
    IReadOnlyList<CriterionSuggestion> SuggestScores(
        JsonElement candidateContent, JsonElement staffContent, Rubric rubric, JsonElement response);
}

public interface ITaskType<TCandidateContent, TStaffContent, TResponse> : ITaskType
{
    Result ValidateContent(TaskContent<TCandidateContent, TStaffContent> content);
    Result ValidateResponse(TResponse response);
    IReadOnlyList<CriterionSuggestion> SuggestScores(
        TaskContent<TCandidateContent, TStaffContent> content, Rubric rubric, TResponse response);
}

public abstract class TaskType<TCandidateContent, TStaffContent, TResponse>
    : ITaskType<TCandidateContent, TStaffContent, TResponse>;

public interface ITaskTypeEndpoints
{
    void MapCandidateEndpoints(IEndpointRouteBuilder taskType);
}

public interface ITaskTypeRegistry
{
    IReadOnlyList<ITaskType> All { get; }
    ITaskType? Find(string key);
}
```

- **Two views of one type.** Modules hold content and responses as `jsonb` and use the untyped `ITaskType` members. A task type implements only the typed ones by deriving from `TaskType<…>`, which reads the JSON with `TaskTypeJson.Options` and turns JSON that doesn't match the models into a `ValidationError` keyed `candidateContent`, `staffContent` or `response`.
- **Strict JSON.** `TaskTypeJson.Options` are the web defaults plus strict numbers ([0018](0018-strict-json-numbers.md)), nullable annotations and required constructor parameters, and unknown properties rejected. A typo in a seed package fails instead of being dropped. Whatever stores content, responses or rubrics serialises them with the same options.
- **Expected failures are `Result`s** with a `ValidationError` keyed by field, which the editor shows next to the field. Exceptions are for bugs only: `SuggestScores` expects content and a response that were already validated.
- **`TaskContent<TCandidate, TStaff>`** carries both parts together, because a type's rules and suggestions often need both (for example a planted secret in the staff part that must appear in a candidate file).
- **`SuggestScores` gets the rubric**, because criteria are authored per task: the type looks up the criterion it can judge, typically one named in its staff content, and suggests nothing when the task has no such criterion. The default suggests nothing.

### Rubric and suggestion models

`Rubric(Criteria)` holds `ScaleCriterion` (`Weight`, optional `Levels`) and `CheckCriterion` (`Expected`, `Critical`), serialised with a `type` discriminator (`scale` / `check`). A `CriterionSuggestion` is a `ScaleSuggestion` (score 0–4) or a `CheckSuggestion` (`yes` / `no`), each with a `Reason` shown to the interviewer. The rubric's own validation rules (at least one `scale` criterion, unique kebab ids, weight 1–10) belong to TaskBank (HLB-47).

### Registration and discovery

- `services.AddTaskType<T>()` registers a type as a singleton `ITaskType`; the same class twice is registered once. `AddTaskTypeRegistry()` registers the registry, also with no types.
- The registry rejects a key that isn't kebab-case, a schema version below 1 and two types with the same key. The API host resolves it while mapping endpoints, so a bad registration stops the API at startup.
- A type that implements `ITaskTypeEndpoints` gets the group `/api/candidate/tasks/<key>`. Its endpoints inherit the candidate group's authorisation, antiforgery and OpenAPI document, and take the session from the cookie ([0008](0008-candidate-access.md)).
- No real type ships in HLB-5. A fake `test-echo` type in `HireLab.Testing`, registered only in test hosts, proves the framework, including a candidate endpoint.

### Leak guard

The candidate OpenAPI document must not contain `referenceAnswer`, `interviewerNotes`, `rubric` or `staffContent` (or `staffOnly`) anywhere in its schemas, nested fields included. A test asserts it, and also that the candidate document shares no schema with the staff document.

### Frontend registries

- The `tasks` lib has three registry entry points: `@hirelab/tasks/candidate`, `@hirelab/tasks/review` and `@hirelab/tasks/editor`. Each maps a task type key to a lazy `loadComponent` (`TaskTypeRegistry`) and is provided through an injection token (`CANDIDATE_TASK_TYPES`, `REVIEW_TASK_TYPES`, `EDITOR_TASK_TYPES`), so app shells can be tested with a stub type.
- `@hirelab/tasks/framework` holds the shared `TaskTypeRegistry` and `TaskContent`. The `candidate` entry point and the framework are shared libs; `review` and `editor` are staff-only, and the lint boundaries keep them out of the candidate app ([0003](0003-repository-and-frontend-layout.md)).
- The component contracts are TypeScript interfaces: `CandidateTaskComponent` (`content`, `response`, `responseChange`), `ReviewTaskComponent` (`content`, `response`, `referenceAnswer`, `interviewerNotes`, `live`) and `EditorTaskComponent` (`content`, `errors`, `contentChange`). Unlike 0013's sketch, the review component gets both content parts (`TaskContent`), since reviewing usually needs what the candidate saw.
- The registries ship empty; the first entries come with HLB-7.

### Storage decisions that follow from the contract

- **Two content columns.** A task version stores `candidate_content` and `staff_content` as two `jsonb` columns, so a candidate-side query can't read staff content by accident.
- **Content schema version per task version.** Each version stores the `ContentSchemaVersion` it was validated with. When it differs from the registered type's, reading the version fails loudly; MVP 1 has no upgrade hook. A breaking content change in a type means a new schema version and, until upgrades exist, new task versions.
- **A task whose type is no longer registered** is left out of the task bank with a warning instead of failing the bank.

### Seeds and drafts

- **Seeds are never drafts.** A seed package is always published; `draft` in `task.json` is rejected by the import. This drops the `draft` field of 0013's folder format.
- **`TaskBank:IncludeDrafts`** and drafts in the bank move to HLB-22 together with the editor. HLB-5's bank shows active versions only.

### Re-pinning is lazy

0013 and 0015 re-pin Pending session tasks when the active version changes. There is no `ActiveVersionChanged` event: the migration worker, which imports seeds and so changes the active version of system tasks, is a separate process and can't raise in-process integration events. Instead a Pending session task follows the active version until it starts. Sessions re-checks the active version, and re-pins with a fresh snapshot, when the session task becomes `Current` or when the queue is opened (HLB-6). Current and Done session tasks keep their version, as before.

## Consequences

- A task type is a small class plus three frontend entry points; it never deals with JSON, registration order or routing.
- The untyped and typed views share one implementation in the base class, so modules and types can't disagree about how content is read.
- Strict JSON makes old stored documents fail loudly when a model changes without a new schema version. That's the intended trade: no silent data loss, at the cost of explicit versioning.
- The registry's keys are a public contract: the backend key, the frontend registry keys, the seed `taskType` field and the endpoint route must match, and a key can never be renamed once tasks use it.
- Lazy re-pinning means a queue may briefly show a Pending task's old version until it is opened or started; nothing is ever re-pinned under a candidate.
- The `any` in the frontend registry types is deliberate: signal inputs are invariant, so a registry of every type can't be typed more precisely. Each component still implements its contract with its own types.

## Status

Accepted
