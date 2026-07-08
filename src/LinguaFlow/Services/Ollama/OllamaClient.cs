namespace LinguaFlow.Services.Ollama;

using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LinguaFlow.Models;

public sealed class OllamaClient
{
    private static readonly Uri DefaultEndpoint = new("http://localhost:11434/");
    private readonly HttpClient httpClient;

    public OllamaClient()
        : this(new HttpClient { BaseAddress = DefaultEndpoint, Timeout = TimeSpan.FromMinutes(5) })
    {
    }

    public OllamaClient(string endpoint)
        : this(new HttpClient { BaseAddress = CreateEndpoint(endpoint), Timeout = TimeSpan.FromMinutes(5) })
    {
    }

    public OllamaClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public void ConfigureEndpoint(string endpoint)
    {
        httpClient.BaseAddress = CreateEndpoint(endpoint);
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<OllamaTagsResponse>("api/tags", cancellationToken);
        return response?.Models.Select(model => model.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray() ?? [];
    }

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var body = new OllamaChatRequest(
            request.Model ?? "mistral-nemo:latest",
            false,
            [
                new OllamaChatMessage("system", BuildSystemPrompt(request)),
                new OllamaChatMessage("user", request.SourceText)
            ]);

        using var response = await httpClient.PostAsJsonAsync("api/chat", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        stopwatch.Stop();

        int? tokenUsage = chatResponse is null ? null : chatResponse.PromptEvalCount + chatResponse.EvalCount;
        return new TranslationResult(chatResponse?.Message?.Content?.Trim() ?? string.Empty, stopwatch.Elapsed, tokenUsage);
    }

    private static string BuildSystemPrompt(TranslationRequest request)
    {
        return $"""
            You are a professional translator.

            Translate the following English into natural, fluent {request.TargetLanguage}.

            Style: {request.Style}.

            Do not explain.
            Do not summarize.
            Preserve paragraphs and lists.
            Maintain professional grammar.
            Output only the translated document.
            """;
    }

    private static Uri CreateEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return DefaultEndpoint;
        }

        var normalized = uri.ToString().TrimEnd('/') + "/";
        return new Uri(normalized);
    }

    private sealed record OllamaTagsResponse([property: JsonPropertyName("models")] OllamaModel[] Models);

    private sealed record OllamaModel([property: JsonPropertyName("name")] string Name);

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("messages")] OllamaChatMessage[] Messages);

    private sealed record OllamaChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaChatMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int EvalCount);
}
