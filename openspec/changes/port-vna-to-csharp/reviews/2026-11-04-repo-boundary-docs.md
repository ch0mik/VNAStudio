# Project boundary and public documentation review

Date: 2026-11-04.

## Reviewed

- Removed README links that pointed outside the repository to local `reference` or `decompiled` siblings. The documentation now names their role and external location without requiring those folders to build the project.
- Removed the user's absolute `C:\Users\...` download path from committed documentation.
- Reworded source notes in the screenshot roadmap and measurement guides; the supplied material remains outside the `VnaStudio` GitHub root.
- Confirmed the workspace has sibling directories `decompiled`, `reference`, and `VnaStudio`; no reference binaries or decompilation output were copied into the project.

## Verification

- Searched Markdown for links targeting `reference` or `decompiled`; no public relative links remain.
- `openspec validate port-vna-to-csharp --strict`: rerun after this review entry.
