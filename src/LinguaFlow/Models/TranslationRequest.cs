namespace LinguaFlow.Models;

/// <summary>
/// Describes a translation request made by the document workspace.
/// </summary>
/// <param name="SourceText">English text to translate.</param>
/// <param name="TargetLanguage">Target language name.</param>
/// <param name="Style">Requested translation style.</param>
/// <param name="Model">Optional model name for model-backed engines.</param>
public sealed record TranslationRequest(string SourceText, string TargetLanguage, string Style, string? Model);
