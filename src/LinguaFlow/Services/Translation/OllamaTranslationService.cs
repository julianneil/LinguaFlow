namespace LinguaFlow.Services.Translation;

using LinguaFlow.Models;
using LinguaFlow.Services.Ollama;

public sealed class OllamaTranslationService : ITranslationService
{
    private readonly OllamaClient ollamaClient;

    public OllamaTranslationService(OllamaClient ollamaClient)
    {
        this.ollamaClient = ollamaClient;
    }

    public string Name => "Ollama";

    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        return ollamaClient.TranslateAsync(request, cancellationToken);
    }
}
