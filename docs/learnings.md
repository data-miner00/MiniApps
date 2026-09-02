## Smoke-test a REPL console app via piped stdin — 2026-08-13

Driving a console REPL's full interaction non-interactively in one shot, instead of typing it in by hand, so you can verify a multi-step flow (compile, match, error, exit) in a single command.

```bash
printf '(?<user>[\\w.+-]+)@(?<domain>[\\w-]+\\.[\\w.-]+)\nContact me at foo.bar@example.com or admin@sub.example.org\npattern\n[invalid(\nexit\n' | dotnet run --project src/RegexTester --no-build 2>&1
```

Each `\n`-separated line in the `printf` becomes one line of stdin the REPL reads via `Console.ReadLine()`. Use `--no-build` after a prior `dotnet build` to skip rebuilding. Reusable for any of this repo's REPL-style console apps (`ODataQuerySimulator`, `CronExpressionSimulator`, `RegexTester`) — swap the piped lines for whatever commands/inputs exercise the flow you want to check.

## Use .NET's inline regex options instead of a custom flags syntax — 2026-08-13

When building a tool around user-supplied regex, don't invent a custom syntax for flags (e.g. a trailing `/i`, `/m` delimiter) — `System.Text.RegularExpressions.Regex` already supports inline option groups directly inside the pattern: `(?i)` case-insensitive, `(?m)` multiline, `(?s)` singleline, `(?x)` ignore-pattern-whitespace. E.g. `(?i)hello` matches "Hello", "HELLO", etc. This sidesteps ambiguity with patterns that legitimately contain `/` (paths, URLs) and means the tool can pass the raw pattern straight to `new Regex(pattern)` with no pre-parsing.

## Prefer `dotnet sln add` over manually editing a .sln file — 2026-08-13

Adding a new project to a `.sln` by hand requires generating a GUID, inserting a `Project(...)...EndProject` block, and adding 12 lines (Debug/Release × Any CPU/x64/x86 × ActiveCfg/Build.0) to the `ProjectConfigurationPlatforms` section — easy to get wrong or leave inconsistent with sibling projects. `dotnet sln <solution>.sln add <path>\<Project>.csproj` does the same thing in one command and guarantees a well-formed result. Reach for the CLI command first; only hand-edit a `.sln` when the CLI isn't available.

## Guard user-supplied regex compilation with a MatchTimeout — 2026-08-13

Any tool that compiles and runs arbitrary user-supplied regex patterns is exposed to catastrophic backtracking (ReDoS) — a pathological pattern + input can hang the match indefinitely. Pass a `TimeSpan` timeout to the `Regex` constructor and catch `RegexMatchTimeoutException` around the match call, e.g. `new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2))`. This is a case where adding defensive error handling is warranted even in a small/lean codebase, because the failure mode is a realistic and direct consequence of the tool's core purpose (running arbitrary patterns), not a hypothetical.
