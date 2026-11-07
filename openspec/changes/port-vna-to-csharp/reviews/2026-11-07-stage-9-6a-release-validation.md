# Stage 9.6a — local release validation

## Verification

- Ran `tests/release/Release.Tests.ps1` locally; all **41** checks passed.
- Checks cover strict release tag parsing, the eleven-RID matrix across workflow, publish CLI and packaging helpers, package naming, completeness validation, checksums, repeatable manifest generation, and corrupt/missing/unexpected assets.
- The script creates and removes only its unique ignored fixture directory under `.artifacts/release-tests/`.
- No release was published and no GitHub workflow was triggered. The remote job/artifact download/platform smoke check in task 9.6 remains open.
- No downloaded application or VNA device was executed or accessed.

Independent reviewer verdict: **PASS** after correcting the description of repeated manifest generation. Remote GitHub execution remains outside this local stage.
