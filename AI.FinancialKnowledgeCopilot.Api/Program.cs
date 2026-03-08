using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Services;
using AI.FinancialKnowledgeCopilot.Infrastructure;
using AI.FinancialKnowledgeCopilot.Worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services
    .AddOptions<IngestionOptions>()
    .BindConfiguration(IngestionOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IPiiDetector, RegexPiiDetector>();
builder.Services.AddScoped<IOutputSafetyFilter, OutputSafetyFilter>();
builder.Services.AddHostedService<Worker>();

if (builder.Environment.IsDevelopment()) //Should have an else block to register Prod implementation, which is not ready yet.
    builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
