# Contributing

## Development-only InMemoryVectorStore
This repository includes an `InMemoryVectorStore` intended for development and testing only. It is not suitable for production due to lack of persistence, indexing, concurrency guarantees, and performance characteristics needed for large datasets.

When running in non-development environments, you must register a production-grade vector store implementation (for example: Redis Vector, Pinecone, Milvus, or FAISS) in the DI container. Example registrations belong in `Program.cs`, and secrets/configuration should be provided via environment variables or a secret store (e.g., Azure Key Vault).

The `InMemoryVectorStore` will throw an exception when constructed in non-development environments to avoid accidental production use.

## Testing and Local Development
- Use `InMemoryVectorStore` for unit tests and lightweight local development.
- For integration tests requiring persistence or scale, prefer running a disposable test instance of Redis/Milvus/FAISS locally or in CI.

## Production Checklist
- Replace `InMemoryVectorStore` with a production vector store implementation before deploying to production.
- Add retries, timeouts, batching, and rate-limit handling for external AI provider calls.
- Add structured logging, metrics, and tracing for ingestion and query pipelines.
- Ensure secrets and provider keys are stored securely.

## Recommended production vector stores
- Redis with RedisAI/Vector indexing
- Pinecone
- Milvus
- FAISS (with persistence layer)