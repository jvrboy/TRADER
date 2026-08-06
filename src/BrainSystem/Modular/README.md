# BrainSystem v1.0

A production-ready intelligent agent system in native C# (.NET 8) featuring:

- **1000+ Neural Network Ensemble** - Feed-forward, LSTM, and 1D CNN networks with SIMD-accelerated matrix operations
- **Hierarchical Memory** - Short-term (circular buffer), Long-term (vector DB), and Episodic (SQLite) memory with consolidation
- **GGUF LLM Runner** - Loads and executes Llama/Mistral/Phi-3 models via LLamaSharp
- **Tool System** - Web search, calculator, data fetcher, code runner, drift predictor, and more
- **REST API** - ASP.NET Core endpoints for training, prediction, chat, and memory queries
- **Training Pipeline** - Trains on Deriv drift switch indices (10, 20, 30) with cross-validation

## Quick Start

### Prerequisites
- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- (Optional) A GGUF model file for LLM features

### Build
```bash
# Windows
.\scripts\build.ps1

# Linux/macOS
chmod +x scripts/build.sh
./scripts/build.sh
```

### Run the API
```bash
cd src/Brain.API
dotnet run
```
The API will be available at `http://localhost:5000`. Swagger UI at `/swagger`.

### Run the Launcher (Interactive Mode)
```bash
cd src/Brain.Launcher
dotnet run
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/train` | Trigger training on drift indices |
| GET | `/api/predict/{index}` | Get prediction for index 10, 20, or 30 |
| POST | `/api/chat` | Chat with the agent (LLM + tools) |
| GET | `/api/memory/query?text=...` | Query long-term memory |
| GET | `/api/status` | System health and status |

## Configuration

Edit `config/appsettings.json` or `src/Brain.API/appsettings.json`:

```json
{
  "ApiKey": "your-api-key-here",
  "GGUF": { "ModelPath": "models/llm_model.gguf" },
  "Ensemble": { "TotalNetworks": 1024 }
}
```

Environment variables override config values (prefix with `Brain__` for nested keys).

## GGUF Model Setup

1. Download a GGUF model (e.g., Phi-3 mini from Hugging Face)
2. Place it in `models/llm_model.gguf`
3. Or set the path in `appsettings.json` under `GGUF:ModelPath`

Without a model, the system still works for predictions and tool usage.

## Architecture

```
BrainSystem/
├── src/
│   ├── Brain.Core/       1000+ NN ensemble (feed-forward, LSTM, CNN)
│   ├── Brain.Memory/     STM, LTM (vector DB), episodic (SQLite)
│   ├── Brain.LLM/        GGUF runner (LLamaSharp)
│   ├── Brain.Tools/      Web search, calculator, data fetcher, etc.
│   ├── Brain.Training/   Deriv API client, data prep, trainer
│   ├── Brain.API/        ASP.NET Core REST API
│   └── Brain.Launcher/   Console host
├── tests/                xUnit test suite
├── config/               Default configuration
├── docs/                 OpenAPI spec
└── scripts/              Build scripts (PowerShell/Bash)
```

## Testing

```bash
dotnet test
```

Tests cover:
- Matrix operations and neural network forward/backward passes
- Memory insertion and retrieval
- Tool execution with mocks
- Training pipeline on synthetic data
- Ensemble performance (1000+ networks)

## License

MIT License - see LICENSE file for details.
