# Container File Listing Parser Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make container file browsing reliable for locale differences and filenames containing whitespace or other separator characters.

**Architecture:** Replace `ls -lap` fixed-column parsing with a POSIX shell listing that emits one NUL-delimited record per entry in the form `<type>\t<basename>\0`. Parse that protocol in a reusable runtime parser and have the existing app service delegate to it, preserving the current UI model and sorting behavior.

**Tech Stack:** C#, .NET 10, POSIX `/bin/sh`, xUnit, FluentAssertions.

---

### Task 1: Add the failing regression contract

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Require the app service to use the runtime file parser and require `LoadFileListAsync` to emit type/name records with NUL delimiters instead of splitting `ls -lap` output on spaces.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerFileListing_ShouldUseDelimitedShellOutputAndServiceParser`

Expected: FAIL because the view model still calls `ls -lap` and parses fixed columns, and the service does not delegate to a runtime file parser.

### Task 2: Add the machine-readable runtime parser

**Files:**
- Create: `src/WinContainers.Runtime/WslcFileParser.cs`
- Modify: `src/WinContainers.App/Services/ContainerService.cs`
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Add parser coverage**

Test NUL-delimited records containing spaces, tabs, and apostrophes, preserving the record type and name exactly.

**Step 2: Implement the parser**

Parse each non-empty NUL-delimited record at its first tab. Accept only `d` and `f` type markers, ignore malformed records, and return directories before files sorted by case-insensitive name.

**Step 3: Delegate from `ContainerService`**

Use `WslcFileParser.Parse` before the existing JSON fallback so the service remains compatible with any existing JSON output.

### Task 3: Replace fixed-column file listing

**Files:**
- Modify: `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:538-604`

**Step 1: Build a delimiter-safe shell command**

Use `/bin/sh`, shell-quote the requested directory, enumerate normal and hidden entries, classify each entry with `[ -d ]`, and emit `d\tname\0` or `f\tname\0`. Use `${entry##*/}` so names are emitted without the parent path.

**Step 2: Map parser output to UI entries**

Call `_containerService.ParseFileEntries(output)`, skip `.` and `..` as before, and assign the existing icons and permission display values.

### Task 4: Verify the fix

**Step 1: Run focused tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~Container`

Expected: PASS.

**Step 2: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: all tests pass.

**Step 3: Build the app project**

Run: `dotnet build src/WinContainers.App/WinContainers.App.csproj -c Debug --nologo -v q`

Expected: build succeeds with no warnings or errors.

**Step 4: Check the diff**

Run: `git diff --check`

Expected: no whitespace errors.
