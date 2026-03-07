using AI.FinancialKnowledgeCopilot.Application;
using AI.FinancialKnowledgeCopilot.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AI.FinancialKnowledgeCopilot.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly IAgentQueryService _queryService;
        public AiController(IAgentQueryService queryService)
        {
            _queryService = queryService;
        }

        [HttpPost]
        public async Task<AgentAnswer> Ask([FromBody] string question)
        {
            return await _queryService.AskAsync(question, CancellationToken.None);
        }
    }
}
