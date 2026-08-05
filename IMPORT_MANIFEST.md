# Project Archive Import Manifest

This repository update imports the **safe, source-oriented contents** of the project-shared archives into `TRADER`. All archives were integrity-checked before extraction, and their paths were reviewed to reject traversal and link-based extraction risks. The source was copied into isolated directories so that the existing MAUI application and backend remain unchanged.

| Metric | Result |
| --- | ---: |
| Project-shared archives reviewed | 12 |
| Files extracted into a review staging area | 175 |
| Files imported into this repository | 95 |
| Imported C# source files | 47 |
| Imported project/solution files | 20 |
| Imported font files | 3 |

## Imported materials

The imported C# projects remain separate from the existing `TraderApp.sln`. This preserves their original solution structure and avoids silently changing build dependencies in the MAUI application.

| Source archive | Repository destination | Contents |
| --- | --- | --- |
| `BrainSystem.tar.gz` | `ImportedProjects/BrainSystemNative/` | Stand-alone .NET 8 cognitive-system source, README, and project file. |
| `BrainSystem.zip` | `ImportedProjects/BrainSystemApi/` | ASP.NET Core/API solution source, tests, docs, configuration template, build scripts, and reproducibility seed. |
| `DsiAgentic_csharp.zip` | `ImportedProjects/DsiAgentic/` | .NET 8 agentic analysis solution, source projects, configuration, documentation, and empty runtime-data placeholders. |
| `freedom-font.zip` | `Assets/Fonts/Freedom/` | Font files, attribution/readme, and source package metadata. The supplied metadata identifies it as CC BY-SA. |
| `to-the-point-font.zip` | `Assets/Fonts/ToThePoint/` | Font file and the included SIL Open Font License. |

## Intentional exclusions

> **Why exclusions exist:** `TRADER` is a public repository. I excluded precompiled artifacts and any third-party font package whose supplied terms either prohibit redistribution/app embedding or do not clearly grant public redistribution rights.

| Excluded material | Reason |
| --- | --- |
| `BrainSystem.zip` packaged `bin/` directory | Precompiled application, dependency, and platform-native build outputs are not source-controlled. The associated source, tests, docs, and configuration are included. |
| Original ZIP and TAR.GZ archives | Their reviewed contents have been extracted and imported in usable source form; retaining duplicate archives would add unnecessary binary duplication. |
| `ariana-violeta-font.zip` | Marked only as “Freeware”; public redistribution rights are not stated in the supplied metadata. |
| `baby-plums-font.zip` | Supplied licence limits use to personal/demo purposes, disallows commercial use and app/server embedding, and prohibits redistribution. |
| `cookie-crisp-font.zip` | Supplied licence limits use to personal/demo purposes, disallows commercial use and app/server embedding, and prohibits redistribution. |
| `debrosee-font.zip` | The supplied package does not provide clear public redistribution terms. |
| `happy-swirly-font.zip` | Supplied licence limits use to personal/demo purposes, disallows commercial use and app/server embedding, and prohibits redistribution. |
| `love-days-love-font.zip` | Supplied licence limits use to personal/demo purposes, disallows commercial use and app/server embedding, and prohibits redistribution. |
| `short-baby-font.zip` | Marked only as “Freeware”; public redistribution rights are not stated in the supplied metadata. |

## Validation notes

No archive was executed. The import staging review found no embedded configuration credentials in the source/configuration files. The `DsiAgentic` runtime directories are retained with `.gitkeep` placeholders so the intended data layout is visible without committing generated trading histories or runtime state.

For the imported subprojects, consult their own README files before integrating them into `TraderApp.sln` or enabling any data-provider configuration.

## Build-validation limitation

The execution environment used for this import does not have the .NET SDK installed, so a compile test could not be run here. A static review of the imported project and properties files found no custom MSBuild `Target`, `Exec`, `UsingTask`, or external `Import` directives. Each imported project should be built in a .NET 8 development environment before being integrated with the application.
