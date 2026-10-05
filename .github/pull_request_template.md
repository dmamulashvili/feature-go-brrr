## What this changes

<!-- One or two sentences. Name the feature(s): e.g. "Adds Orders/CancelOrder". -->

## Review checklist ([AGENTS.md](../AGENTS.md))

### File and structure

- [ ] One new file at `Features/{Area}/{Verb}{Noun}.cs`, class name = file name (R1)
- [ ] No reference to another feature (R2)
- [ ] One new line in `AllFeatures.cs` (R3)
- [ ] No new service, repository, interface, base class or helper folder (R13, R15)

### Inside the feature

- [ ] Order: Contract → `Map` → `Handle` → phases (R5)
- [ ] `Handle` only calls phases and returns results (R6)
- [ ] Phase names only from the phase table (R7)
- [ ] No phase calls another phase (R8)
- [ ] Select → Legit check → Apply the Sauce → Yeet, nothing written before checks (R10)
- [ ] `ApplySauce` has no `await` and no `db` parameter (R11)
- [ ] No empty or unused phases (R12)
- [ ] Under ~300 lines (R17)

### Tests and build

- [ ] Matching test file exists (R16)
- [ ] Happy path + one test per check
- [ ] `dotnet build` and `dotnet test` pass

**Intern test:** can someone new explain what the feature does by reading only `Handle`?
