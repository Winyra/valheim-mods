# Copilot Instructions

## Project Overview
Valheim client and server mods built with C#, BepInEx 5.4.x, and Unity (.NET Framework 4.6.2 and .NET Standard 2.0).
Included mods:
- `WorldCompareScreenshots`: Multi-world aligned screenshot comparison mod with waypoint storage and automated capture sequences.
- `TreeTransparency`: In-game hotkey toggle to make trees and foliage semi-transparent during building.

## Build & Test
- **Build**: `dotnet build valheim-mods.slnx`
- **Test**: `dotnet test`
- **Lint**: `dotnet format --verify-no-changes`
- Always verify build and test before committing changes.

## Code Style
- Use `const` by default; `let` only when reassignment is required.
- Named exports only from utility files. No default exports except for pages/components.
- No `any` types — use `unknown` and narrow.
- In C#: use explicit or clear types, avoid unnecessary allocations, keep methods focused.
- Max function length: 40 lines. Extract if longer.

## File Navigation (Anti-Loop Guardrail)
- Do not re-read a file you have already read in this session. If you need to re-verify a specific section, state which lines and why before doing so.
- Before reading any file, check whether its contents are already in your context from a prior read.
- If you find yourself about to read the same file for the third time in a session, stop and summarize what you already know from it, then proceed with what you have.
- Prefer `#filename` references provided by the user over searching for files.

## Testing
- Every new function must have a corresponding test.
- Test user behavior, not implementation details.

## Prohibited Patterns
- No `console.log` in production code.
- No `@ts-ignore` or `@ts-expect-error` without a comment explaining why.
- Never hardcode secrets, API keys, or tokens. Use environment variables.
- Do not install new dependencies without confirming with the user first.

## Scope Discipline
- Limit edits to files directly required by the task.
- Do not perform adjacent refactors, cleanups, or "while I'm here" changes unless explicitly asked.
- If a task requires modifying more than 3 files, state the plan before executing.
