namespace LinguaFlow.Models;

public sealed record TranslationResult(string Text, TimeSpan Latency, int? TokenUsage);
