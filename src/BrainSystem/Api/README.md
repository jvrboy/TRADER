# Brain System v1.0

Brain System is a source-available .NET 8 starter platform for experimenting with a deterministic ensemble of custom neural-style models, hierarchical memory, responsible tool orchestration, and a local REST API.

> **Important:** This project is an engineering and research starter, **not a trading bot**. It does not place trades, it does not connect to brokerage accounts, and it makes no prediction or profitability claims. Market output must be independently validated before any real-world use.

## What is included

- 1,024 lightweight custom C# networks, covering feed-forward, recurrent, and convolution-inspired model families.
- Weighted ensemble inference and a deterministic, reproducible synthetic training pipeline.
- Short-term circular memory, episodic SQLite audit storage, and a lightweight long-term semantic retrieval index.
- Safe tool registry: calculator, UTC time, unit conversion, market-data abstraction, and predictor.
- Optional GGUF-provider boundary for adding a local LLM provider without bundling any proprietary or large model files.
- ASP.NET Core API with validation, structured logging, rate limiting, health checks, API-key protection for mutation endpoints, and Swagger in development.
- xUnit unit and API integration tests.
- A build script that restores, tests, publishes, and creates a release ZIP.

## Quick start

```bash
dotnet restore BrainSystem.sln
dotnet test BrainSystem.sln --configuration Release
dotnet run --project src/BrainSystem.Api
```

Then open `http://localhost:5080/swagger` in development.

## Build a distributable archive

Linux/macOS:

```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

Windows PowerShell:

```powershell
./scripts/build.ps1
```

Both scripts produce `dist/BrainSystem.zip`. The static page in `download/index.html` expects `BrainSystem.zip` next to it.

## Optional data and models

- The default `SyntheticMarketDataSource` deliberately avoids live market calls so examples are reproducible and do not require credentials.
- Implement `IMarketDataSource` for a provider that you are authorized to use. Configure it with environment variables, not source code.
- `GgufLlmProvider` validates a configured `.gguf` path and offers a stable integration boundary. Add a maintained local inference adapter in your deployment environment; no GGUF weights are bundled.
- `models/ensemble.seed` is a small deterministic seed for reproducibility, not a trained market model.

## API overview

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/status` | Runtime and model readiness |
| `POST` | `/api/train` | Runs deterministic synthetic ensemble training |
| `GET` | `/api/predict/{index}` | Produces a bounded research prediction for 10, 20, or 30 |
| `POST` | `/api/chat` | Handles a message with safe, explicitly parsed tool calls |
| `GET` | `/api/memory/query?q=...` | Retrieves relevant consolidated memories |
| `GET` | `/healthz` | Liveness health check |

`POST /api/train` requires `X-Api-Key` when `Security:ApiKey` is set. Leave it empty only for local development.

## Operational notes

- Set `ASPNETCORE_ENVIRONMENT=Production`, configure `Security:ApiKey`, and put the API behind TLS before exposing it beyond a trusted network.
- Configure persistent SQLite storage with `Memory:EpisodicDatabasePath`.
- Use an external observability destination in production; JSON console logs are ready for collection.
- The process never executes arbitrary C# or Python. That exclusion is intentional: unrestricted code execution is not safe for a public agent API.

See [`docs/OPERATIONS.md`](docs/OPERATIONS.md) and [`docs/API.md`](docs/API.md) for details.