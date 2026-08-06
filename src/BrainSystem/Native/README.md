# BrainSystem — Native C# Cognitive AI

A complete, self-contained cognitive architecture in pure C# (.NET 8):

- **1,100 neural networks** across 17 biologically-inspired cortical regions
- **Multi-tier memory system** (Working / Short-term / Long-term / Episodic / Semantic / Procedural)
- **Knowledge graph** for structured facts
- **GGUF runner** — native parser for llama.cpp `.gguf` model files (F32/F16/BF16/Q4_0/Q4_1/Q8_0 dequant)
- **Tool system** (13 built-in tools) + **Function registry** (10 native functions)
- **SIMD-accelerated tensor math** — no external ML dependencies
- **REPL shell** with slash-commands + `--test` self-test mode

## Quick start
```bash
dotnet build -c Release
dotnet run -c Release -- --test                  # self-test
dotnet run -c Release                            # interactive REPL
dotnet run -c Release -- --gguf model.gguf       # with an LLM loaded
```

## REPL commands
```
/stats                 system snapshot
/tools                 list registered tools
/tool <name> k=v k=v   invoke a tool  (e.g. /tool calculator expression=2*(3+4)^2)
/fn <name> k=v k=v     invoke a native function
/mem <query>           semantic memory search
/store <text>          store a memory
/region <name>         info about a brain region
/consolidate           sleep-consolidation pass
/save <dir>            persist all state
/gguf <path>           load a GGUF model at runtime
```

## Layout
```
Core/          Tensor (SIMD), Activation, Brain (top-level orchestrator)
NeuralNetworks/  NeuralNetwork (MLP+backprop), NetworkRegistry, BrainFactory
Memory/        MemorySystem (6 tiers + decay + consolidation), KnowledgeGraph
LLM/           GgufFile, GgufDequant, GgufRunner (retrieval-augmented generation)
Tools/         ToolSystem + 13 built-in Tool subclasses
Functions/     FunctionRegistry (10 native delegates)
Program/       Program.cs — REPL entrypoint
```

## Cortical regions built by default (BrainFactory)
visual_cortex (120), auditory_cortex (80), somatosensory (60), olfactory (20),
wernicke_area (80), broca_area (80), semantic_net (120),
motor_cortex (60), cerebellum (60),
hippocampus (80), entorhinal (40),
prefrontal_cortex (80), anterior_cingulate (40),
amygdala (30), nucleus_accumbens (20),
world_model (100), meta_controller (30)  → **1,100 networks**

## Built-in tools
calculator, datetime, file_read, file_write, list_dir, http_fetch, regex,
memory_store, memory_recall, brain_forward, system_stats, echo, random

Add your own by subclassing `Tools.Tool` and calling `brain.Tools.Register(...)`.

## GGUF support
`LLM/GgufFile.cs` parses the full GGUF v2/v3 header (magic, metadata KV pairs, tensor
index). `GgufDequant.cs` dequantises F32/F16/BF16/Q8_0/Q4_0/Q4_1 blocks to `float[]`.
`GgufRunner.cs` reconstructs the vocab, tokeniser and metadata, then runs
retrieval-augmented generation grounded on the brain's memory. Extend it with a full
transformer forward pass by calling `LoadTensor("blk.0.attn_q.weight")` etc.

## Persistence
`brain.Save("state/")` writes every network as `state/networks/<id>.bnn`, memories to
`memory.json`, and the knowledge graph to `knowledge.json`. Reload later with the
registry's `LoadAll(...)` and memory's `Load(...)`.
