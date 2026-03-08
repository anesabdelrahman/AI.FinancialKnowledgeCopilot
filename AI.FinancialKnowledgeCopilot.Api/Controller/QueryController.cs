using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AI.FinancialKnowledgeCopilot.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]    
    public class QueryController : ControllerBase
    {
        private readonly IQueryService  _queryService;
        public QueryController(IQueryService queryService)
        {
            _queryService = queryService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] QueryRequest request, CancellationToken cancellationToken)
        {
            var response = await _queryService.AskAsync(request, cancellationToken);

            return Ok(response);
        }
    }
}
