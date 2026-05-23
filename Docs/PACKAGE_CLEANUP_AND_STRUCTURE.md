# Package cleanup & structure — Guide

Goal: convert the current folder into a clean, well-structured Unity package that can be published or consumed as a local package. This guide walks through auditing/removing dead scripts, creating a package layout, adding `package.json` and assembly definition guidances, and validating the package in Unity.

Prerequisites
- A working git repository for the project (recommended).
- Unity Editor matching your project's `ProjectVersion.txt`.

Checklist (high-level)
- [ ] Audit and identify dead / unused scripts.
- [ ] Move runtime code into a package folder `com.acproject.scoperuntimemonitoring`.
- [ ] Add `package.json`, `README.md`, `CHANGELOG.md`, `LICENSE`.
- [ ] Create `Runtime/`, `Editor/`, `Tests/`, `Documentation/`, `Samples/` subfolders.
- [ ] Add/adjust Assembly Definition (`.asmdef`) files for runtime and editor assemblies.
- [ ] Ensure `Resources/` and `UI Document` assets are in the package runtime path.
- [ ] Validate compilation & test in Unity, fix references.

1) Audit and remove dead scripts (safe-first)
- Run a quick search in the repo for obvious unused types (e.g. classes never referenced). Use your IDE/`rg`/`grep`.
- Create a branch `package/cleanup`.
- Move questionable files into a temporary folder `archive/` inside the repo (keep `.meta` files if you want to preserve GUIDs temporarily), for example `Assets/Archive/Removed-YYYYMMDD/`.
- Commit the branch, run Unity to verify compilation. If nothing breaks after a couple of runs, you can remove archived files permanently.

2) Create package root folder (recommended path)
- At repository root create the folder: `com.acproject.scoperuntimemonitoring/`
- Inside it create these folders:
  - `Runtime/` — runtime C# scripts, `Resources`, runtime UI assets (UIDocument prefabs, USS, UXML).
  - `Editor/` — editor-only scripts, inspectors, editor windows.
  - `Tests/` — editmode/playmode tests.
  - `Documentation/` — user-facing docs and samples (or keep in `Docs/`).
  - `Samples/` — optional sample scenes and assets.

3) Add `package.json` (example)
- Create `com.acproject.scoperuntimemonitoring/package.json` with contents like:

```json
{
  "name": "com.acproject.scoperuntimemonitoring",
  "displayName": "Scope Runtime Monitoring",
  "version": "0.1.0",
  "unity": "2021.3",
  "description": "Runtime monitoring UI toolkit for debugging variables and components.",
  "keywords": ["debug","monitoring","runtime","ui"],
  "author": { "name": "AC Project" },
  "dependencies": {}
}
```

Adjust `unity` to the minimum Unity version you support.

4) Move files into package structure
- Move runtime scripts and assets from `Assets/Scope/...` into `com.acproject.scoperuntimemonitoring/Runtime/Scope/...` preserving relative namespaces.
- Move editor scripts into `com.acproject.scoperuntimemonitoring/Editor/...`.
- Keep the `Resources/` folder if runtime code loads assets via `Resources.Load` inside the package runtime folder.
- If you have prefab UXML/USS assets used by the runtime UI, place them under `Runtime/Resources/` (or `Runtime/UIDocuments/`), then update references if necessary.

5) Add Assembly Definitions
- Add `com.acproject.scoperuntimemonitoring.Runtime.asmdef` in the package `Runtime/` folder. Minimal content:

```json
{
  "name": "ScopeRuntimeMonitoring.Runtime",
  "rootNamespace": "ScopeRuntimeMonitoring",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true
}
```

- Add `com.acproject.scoperuntimemonitoring.Editor.asmdef` in `Editor/` and set `Editor` in the platforms and reference the runtime asmdef.

6) Fix script references and namespaces
- Open Unity and allow it to compile. Resolve any missing namespace or assembly reference errors by adding asmdef references (e.g. to `UnityEngine.UIElements` or `UnityEditor` in Editor asmdefs).

7) Ensure resources and style assets are accessible at runtime
- If you used `Resources.Load("RuntimeMonitorUILayout")` before, ensure the UXML/UIDocument prefab and its `StyleSheet` are located under `Runtime/Resources/` in the package so `Resources.Load` still works.

8) Add documentation and samples
- Add README.md at package root and a `Documentation~/` folder for in-package docs. Link to example scenes in `Samples/`.

9) Validate and iterate
- In your repo root, run Unity, open the sample scene and ensure the monitors create boxes as before.
- Test Editor-only features still compile by running editmode tests or opening inspector windows.

10) Publishing / Local testing
- To test the package locally, in a consumer project add this to `manifest.json` (Package Manager):

```json
"com.acproject.scoperuntimemonitoring": "file:../path/to/com.acproject.scoperuntimemonitoring"
```

- To publish to scoped registry or OpenUPM, follow their publishing guidelines.

Notes & tips
- Keep `asmdef` names stable; they determine API surface and references.
- Preserve GUIDs for critical assets if you want existing scene/prefab references to continue working — moving assets between `Assets/` and a package will change GUIDs unless you copy `.meta` files. For a clean package, it's usually better to accept new GUIDs and update references.
- Prefer `Resources` only when necessary; for assets wired on prefabs via serialized references, keep the prefab as part of the package and reference its internal assets.
- Use an `Archive/` folder for safe deletion (commit, test, then delete permanently once happy).

Example minimal file list for package root
- `com.acproject.scoperuntimemonitoring/package.json`
- `com.acproject.scoperuntimemonitoring/README.md`
- `com.acproject.scoperuntimemonitoring/Runtime/Scope/*.cs`
- `com.acproject.scoperuntimemonitoring/Runtime/Resources/RuntimeMonitorUILayout.prefab`
- `com.acproject.scoperuntimemonitoring/Runtime/UIDocuments/*.uxml` and `*.uss`
- `com.acproject.scoperuntimemonitoring/Editor/*.cs`
- `com.acproject.scoperuntimemonitoring/Tests/` (optional)

If you want, I can:
- Create a `package.json` scaffold in the repo and suggest an initial asmdef pair. (I can patch these files if you want me to.)
- Generate a short script to move the current `Assets/Scope` files into the package folder while preserving `.meta` where possible.

---
Follow-up: tell me whether you want me to scaffold `package.json` and asmdef files now (I can create them in `com.acproject.scoperuntimemonitoring/`).
