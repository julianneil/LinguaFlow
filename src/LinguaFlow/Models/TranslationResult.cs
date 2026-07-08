namespace LinguaFlow.Models;

/// <summary>
/// Contains translated text and request metadata for status reporting.
/// </summary>
/// <param name="Text">Translated output text.</param>
/// <param name="Latency">Elapsed request time.</param>
/// <param name="TokenUsage">Optional token usage reported by the engine.</param>
public sealed record TranslationResult(string Text, TimeSpan Latency, int? TokenUsage);
