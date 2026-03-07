using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Application;

public class AgentQueryService : IAgentQueryService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILlmClient _lmClient;

    public AgentQueryService(IEmbeddingService embeddingService, 
        IVectorStore vectorStore, 
        IPromptBuilder promptBuilder, 
        ILlmClient lmClient)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _promptBuilder = promptBuilder;
        _lmClient = lmClient;
    }

    public async Task<AgentAnswer> AskAsync(string question, CancellationToken cancellationToken)
    {
        var vector = await _embeddingService.GenerateAsync(question, cancellationToken);
        var context = await _vectorStore.SearchAsync(vector, 5, cancellationToken);
        var prompt = _promptBuilder.Build(question, cancellationToken);

        return await _lmClient.GenerateAsync(prompt, cancellationToken);    
    }
}
