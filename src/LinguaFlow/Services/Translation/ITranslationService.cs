namespace LinguaFlow.Services.Translation;

using LinguaFlow.Models;

/// <summary>
/// Translates English document text into another language.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Gets the display name shown in the translation engine selector.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Translates the supplied document text.
    /// </summary>
    /// <param name="request">Translation request details.</param>
    /// <param name="cancellationToken">Token used to cancel in-flight work.</param>
    /// <returns>The translated result.</returns>
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);
}
