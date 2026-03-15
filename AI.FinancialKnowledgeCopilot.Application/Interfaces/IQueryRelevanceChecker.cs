using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

/// <summary>
/// Determines whether an incoming query is relevant to the financial domain
/// before it is allowed to proceed through the RAG pipeline.
/// Rejects off-topic queries early, saving embedding and LLM costs.
/// </summary>
public interface IQueryRelevanceChecker
{
    RelevanceCheckResult Check(string query);
}
