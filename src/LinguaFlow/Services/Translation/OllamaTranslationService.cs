namespace LinguaFlow.Services.Translation;

using LinguaFlow.Models;
using LinguaFlow.Services.Ollama;

/// <summary>
/// Translation service backed by a local Ollama model.
/// </summary>
public sealed class OllamaTranslationService : ITranslationService
{
    private readonly OllamaClient ollamaClient;

    /// <summary>
    /// Creates a translation service for local Ollama requests.
    /// </summary>
    /// <param name="ollamaClient">Client used to call Ollama.</param>
    public OllamaTranslationService(OllamaClient ollamaClient)
    {
        this.ollamaClient = ollamaClient;
    }

    /// <inheritdoc />
    public string Name => "Ollama";

    /// <inheritdoc />
    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        return ollamaClient.TranslateAsync(request, cancellationToken);
    }
}
