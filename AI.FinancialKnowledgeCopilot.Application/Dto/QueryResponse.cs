namespace AI.FinancialKnowledgeCopilot.Application.Dto
{
    public class QueryResponse
    {
        public string Answer { get; set; } = string.Empty;
        public IEnumerable<string> Sources { get; set; } = [];
    }
}
