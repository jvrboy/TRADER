# Monorepo Extraction & Import Manifest

This repository organization imports, extracts, and organizes the complete ecosystem of projects, tools, engines, libraries, datasets, and typography assets into a clean, unified monorepo structure.

---

## Monorepo Inventory Summary

| Category | Item Count | Key Components |
|---|---:|---|
| **C# .NET Projects** | 42 | `TraderApp` (MAUI), `Trader.Backend`, `BrainSystem` (Native, Api, Modular), `DsiAgentic` (14 subprojects), `NexusBrain` (10 subprojects), `AiBrain`, `AetherBrain`, Test Suites |
| **Developer & Strategy Tools** | 2 | `WinRunner` (WindowsAppRunner GUI/CLI/Core), `java-sandbox` |
| **Trading & Swarm Engines** | 2 | `deriv-ai-swarm` (500 agents × 1,145 indicators), `divergence-system` (Python MTF scanner) |
| **Core Libraries** | 2 | `libs/jcharts` (TradingView lightweight charts), `libs/microkernel` (EventBus, Fibers, Lock-Free Queues) |
| **Market Datasets** | 100+ CSVs | Historical OHLC data for Synthetics (Volatility, Boom, Crash, Jump, Step) and Forex Majors |
| **Typography Packages** | 21 | Full custom font packages with license and metadata files under `assets/fonts/` |
| **Master Solutions** | 3 | `TRADER.sln` (Master unified solution), `src/NexusBrain/NexusBrain.sln`, `tools/WinRunner/WindowsAppRunner.sln` |

---

## Directory Layout & Source Mappings

| Source Component | Repository Destination | Description |
|---|---|---|
| `TraderUI` / `trader_updated` | `src/TraderApp/` | .NET MAUI cross-platform application (Android, iOS, macOS, Windows) |
| `Trader.Backend` | `src/Trader.Backend/` | Agentic backend server with 22 specialized tools and Swarm framework |
| `BrainSystem.tar.gz` | `src/BrainSystem/Native/` | Standalone .NET 8 cognitive system with GGUF runner, tensors, neural networks, and knowledge graph |
| `BrainSystem.zip` | `src/BrainSystem/Api/` | ASP.NET Core API + Core domain model + xUnit test suite |
| `BrainSystem 2.zip` | `src/BrainSystem/Modular/` | Modular cognitive architecture (`Brain.Core`, `Brain.Memory`, `Brain.LLM`, `Brain.Tools`, `Brain.Training`, `Brain.API`, `Brain.Launcher`) |
| `DsiAgentic_csharp.zip` | `src/DsiAgentic/` | .NET 8 agentic analysis engine across 14 dedicated project layers |
| `nexusbrain_agentic_ai_brain-*.zip` | `src/NexusBrain/` | Self-learning neural agentic brain engine with Forex analyzer and multi-agent colony |
| `AI_Brain.zip` | `src/AiBrain/` | Multi-memory neural network engine with Hebbian, Backprop, and Reinforcement learning |
| `aether-brain-native-csharp.zip` | `src/AetherBrain/` | Cognitive memory and adaptive weight orchestration engine |
| `winrunner_src-*.zip` | `tools/WinRunner/` | WindowsAppRunner tool (Core, CLI, Avalonia GUI, Tests) |
| `java-sandbox.zip` | `tools/java-sandbox/` | High-security Java strategy execution sandbox with CLI and scripting engine |
| `divergence-system.zip` | `engines/divergence-system/` | Python Multi-Timeframe Divergence Scanner & Analysis Engine |
| `deriv-ai-swarm-*.zip` | `engines/deriv-ai-swarm/` | Java AI Swarm with 500 agents and 1,145 technical indicators |
| `jcharts-tradingview-library.zip` | `libs/jcharts/` | High-performance Java TradingView Lightweight Charts and technical indicators library |
| `microkernel-system-v1.0.0.zip` | `libs/microkernel/` | Zero-allocation Java microkernel with lock-free data structures and fiber threading |
| `deriv_historical_data.zip` | `data/historical/` | Full tick and OHLC historical market datasets for Deriv synthetics and major forex pairs |
| `files 2.zip` | `docs/deriv/` & `scripts/` | Deriv API documentation, skill specifications, symbol guides, and JavaScript helpers |
| 21 Font Packages | `assets/fonts/` | All 21 font families with TrueType/OpenType files, license text, and metadata |

---

## Verification & Integrity Checks

1. **Project References:** All internal `.csproj` ProjectReference paths have been statically checked and normalized.
2. **Solution Structure:** The master `TRADER.sln` cleanly references all 42 source and test projects categorized by Visual Studio Solution Folders.
3. **Typography & Licensing:** All 21 font families are preserved with their original attribution, license PDF/text files, and documented in `docs/fonts/FONTS.md`.
4. **Artifact Cleanliness:** Build outputs (`bin/`, `obj/`, `target/`, precompiled `.dll` / `.jar`) are excluded and ignored via standard `.gitignore` rules.
