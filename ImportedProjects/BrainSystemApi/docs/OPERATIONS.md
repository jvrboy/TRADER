# Operations guide

## Production baseline

1. Use .NET 8 LTS and apply operating-system security updates.
2. Set `ASPNETCORE_ENVIRONMENT=Production`.
3. Set a strong `Security__ApiKey` in your deployment secret store.
4. Put the API behind a TLS-terminating reverse proxy with an allowlisted origin/network policy.
5. Persist `Memory__EpisodicDatabasePath` on an encrypted volume and define a retention policy.
6. Collect JSON logs and alerts externally. Never log keys, model content, or sensitive user prompts.
7. Keep model weights and third-party data rights separate from this source archive.

## Provider boundaries

`SyntheticMarketDataSource` is intentional. To use provider data, implement `IMarketDataSource` with strict timeouts, validation, provider terms compliance, and an explicit fallback/failure mode. Do not scrape or embed provider credentials.

The GGUF path is also an integration boundary. Place a licensed `.gguf` model outside the source archive, set `Llm__GgufModelPath`, and wire in a maintained, platform-specific local runner. Test that runner in an isolated environment before exposing it to user traffic.

## Safety

The code contains no generic code-execution endpoint and no trading execution. Keep it that way unless a dedicated security review, sandbox, authorization model, audit trail, and threat model are completed first.