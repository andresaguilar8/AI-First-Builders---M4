#!/usr/bin/env bash
# Lista los criterios de aceptación AC-01 a AC-68 del PRD que no tienen al menos un test que los
# referencie y falla si falta alguno (SC-006, principio I de la constitución).
#   - Backend: [Trait("AC", "AC-xx")] en backend/tests.
#   - Frontend: "AC-xx" en el nombre de un it(...)/it.each(...) en frontend/tests.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
backend_tests="$root/backend/tests"
frontend_tests="$root/frontend/tests"

covered="$(
  {
    grep -rhoE --include='*.cs' 'Trait\("AC", "AC-[0-9]{2}"\)' "$backend_tests" | grep -oE 'AC-[0-9]{2}'
    grep -rhE --include='*.ts' --include='*.tsx' "^\s*it(\.each\(.*\))?\(" "$frontend_tests" -A1 \
      | grep -oE 'AC-[0-9]{2}'
  } | sort -u
)"

missing=()
for n in $(seq -w 1 68); do
  grep -qx "AC-$n" <<<"$covered" || missing+=("AC-$n")
done

if ((${#missing[@]} > 0)); then
  echo "Criterios de aceptación sin test (${#missing[@]}):"
  printf '  %s\n' "${missing[@]}"
  exit 1
fi

echo "Los 68 criterios de aceptación tienen al menos un test."
