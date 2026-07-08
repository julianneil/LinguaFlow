namespace LinguaFlow.Services.Translation;

using LinguaFlow.Models;

public interface ITranslationService
{
    string Name { get; }

    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);
}
