#!/usr/bin/env pwsh
# Adds new seed package versions to src/Tasks/tasks.lock.json (ADR 0030) by running the lock guard in update mode.
# Locked versions are never changed: an edited or removed published version still fails.
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repositoryRoot 'tests/HireLab.Tasks.Tests/HireLab.Tasks.Tests.csproj'

$env:HIRELAB_UPDATE_TASKS_LOCK = '1'
try {
    dotnet test --project $testProject
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Remove-Item Env:HIRELAB_UPDATE_TASKS_LOCK -ErrorAction SilentlyContinue
}

Write-Host 'tasks.lock.json is up to date. Commit it with the new seed packages.'
