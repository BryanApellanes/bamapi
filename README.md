# bamapi

A scaffolded-but-unimplemented API host in the Bam Toolkit.

## Overview

`bamapi.sln` declares a `bamapi` project (`bamapi\bamapi.csproj`) and a nested `bam.core` submodule, but the `bamapi.csproj` file — and the rest of the `bamapi` project directory — do not exist in the repository. The only real content checked in is the solution file and a `.gitmodules` entry for `submodules/bam.core`, which points at the legacy `Bam.Core` repository (in turn depending on the legacy `bam.net.shared` shared project). No migrated `Bam.*` code exists here yet.

This repository is registered as one of the bamtk framework's submodules, but as it stands it is an empty scaffold rather than a working project: the solution cannot currently build, since its one referenced project is missing. Treat this as intended-but-not-yet-built work (per the toolkit's convention of leaving stubs in place to signal future intent), not an abandoned or cancelled path.

**Intended role:** `bamapi` and `bamsvc` are planned to split the server side of the Bam protocol stack by audience. `bamsvc` is the internal backend application communication service layer — it hosts BAM protocol endpoints intended for use *between* Bam-based applications/services. `bamapi` is intended to become the public-facing **WebService host** — exposing Bam services over plain HTTP for consumption by external/public API clients (built on `bam.server`'s `[WebService]`/`WebServiceRegistry` hosting, the same way `bamsvc` is). That role has not been built yet; this repository is currently just the unfulfilled placeholder for it.

## Known Gaps / Not Yet Implemented

- `bamapi.csproj` (and any source under a `bamapi/` project folder) does not exist — the solution's own project reference is broken.
- The only dependency wired up (`submodules/bam.core` → `Bam.Core`/`Bam.Net.Shared`) is legacy, pre-`Bam.*`-migration code.
- Last pushed 2024-12-20 — no activity since.
