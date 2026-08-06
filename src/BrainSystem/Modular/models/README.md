# BrainSystem Models Directory

Place your GGUF model file here as `llm_model.gguf`.

## Recommended Models

- **Phi-3 Mini** (3.8B, Q4_K_M) - Small, fast, good for general use
- **Llama-3.2-1B** (1B, Q4_K_M) - Very small, minimal resource usage
- **Mistral-7B-Instruct** (7B, Q4_K_M) - Higher quality, more resources

## Download Sources

- Hugging Face: https://huggingface.co/models?library=gguf
- TheBloke: https://huggingface.co/TheBloke

## Without a Model

The system functions without a GGUF model file:
- Neural ensemble predictions work fully
- All tools are available
- Chat mode returns tool descriptions instead of LLM responses
- Set `GGUF:ModelPath` in `appsettings.json` to point to your model
