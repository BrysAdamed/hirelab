#!/usr/bin/env bash
# Generates the Bicep from the Aspire AppHost and builds and lints it together with the hand-written infra/.
# Usage: eng/validate-infra.sh [output-dir]. Set BICEP to a bicep executable to override discovery.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/infra}"

bicep() {
  if [[ -n "${BICEP:-}" ]]; then
    "$BICEP" "$@"
  elif command -v bicep > /dev/null; then
    command bicep "$@"
  elif command -v az > /dev/null; then
    local verb="$1" file="$2"
    shift 2
    az bicep "$verb" --file "$file" "$@"
  else
    echo "Bicep CLI not found. Install it or set BICEP." >&2
    exit 1
  fi
}

validate() {
  local file="$1"
  echo "Validating $file"
  bicep build "$file" --stdout > /dev/null
  bicep lint "$file"
}

rm -rf "$output"
mkdir -p "$output"

cd "$root"
dotnet tool restore
dotnet aspire publish \
  --apphost "$root/src/HireLab.AppHost/HireLab.AppHost.csproj" \
  --output-path "$output" \
  --non-interactive \
  --nologo

validate "$output/main.bicep"

for file in "$root"/infra/*.bicep; do
  validate "$file"
done

echo "Infrastructure is valid."
