namespace LinguaFlow.Models;

public sealed record TranslationRequest(string SourceText, string TargetLanguage, string Style, string? Model);
