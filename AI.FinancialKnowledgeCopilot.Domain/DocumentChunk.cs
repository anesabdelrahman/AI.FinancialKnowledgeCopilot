namespace AI.FinancialKnowledgeCopilot.Domain;

public class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = Array.Empty<float>();

    public string Title { get; set; } = string.Empty;
}
