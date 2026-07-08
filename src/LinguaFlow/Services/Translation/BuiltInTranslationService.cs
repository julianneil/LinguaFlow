namespace LinguaFlow.Services.Translation;

using System.Diagnostics;
using System.Text.RegularExpressions;
using LinguaFlow.Models;

public sealed partial class BuiltInTranslationService : ITranslationService
{
    private static readonly IReadOnlyDictionary<string, string> PhraseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["hello"] = "hola",
        ["good morning"] = "buenos dias",
        ["good afternoon"] = "buenas tardes",
        ["good evening"] = "buenas noches",
        ["thank you"] = "gracias",
        ["please"] = "por favor",
        ["meeting"] = "reunion",
        ["schedule"] = "programar",
        ["next Tuesday"] = "el proximo martes",
        ["I would like"] = "me gustaria",
        ["sincerely"] = "atentamente"
    };

    public string Name => "Built-in";

    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();

        var translatedText = request.SourceText;
        foreach (var phrase in PhraseMap.OrderByDescending(item => item.Key.Length))
        {
            translatedText = ReplacePhrase(translatedText, phrase.Key, phrase.Value);
        }

        if (string.Equals(translatedText, request.SourceText, StringComparison.Ordinal))
        {
            translatedText = "Built-in translation did not find a matching phrase yet. Select Ollama for full natural translation.";
        }

        stopwatch.Stop();
        return Task.FromResult(new TranslationResult(translatedText, stopwatch.Elapsed, null));
    }

    private static string ReplacePhrase(string text, string source, string replacement)
    {
        return Regex.Replace(text, $@"\b{Regex.Escape(source)}\b", replacement, RegexOptions.IgnoreCase);
    }
}
