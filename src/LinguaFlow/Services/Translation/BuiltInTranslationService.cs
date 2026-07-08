namespace LinguaFlow.Services.Translation;

using System.Diagnostics;
using System.Text.RegularExpressions;
using LinguaFlow.Models;

public sealed partial class BuiltInTranslationService : ITranslationService
{
    private static readonly IReadOnlyDictionary<string, string> SentenceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["hello"] = "Hola",
        ["hello."] = "Hola.",
        ["thank you"] = "Gracias",
        ["thank you."] = "Gracias.",
        ["thank you very much."] = "Muchas gracias.",
        ["please let me know."] = "Por favor, avíseme.",
        ["i like you."] = "Me caes bien.",
        ["i would like to schedule a meeting next tuesday."] = "Me gustaría programar una reunión para el próximo martes.",
        ["i am going to get used to waking up early."] = "Voy a acostumbrarme a madrugar."
    };

    private static readonly IReadOnlyList<(Regex Pattern, string Replacement)> PatternRules =
    [
        (new Regex(@"\bI would like to schedule a meeting\b", RegexOptions.IgnoreCase), "Me gustaría programar una reunión"),
        (new Regex(@"\bI would like to\b", RegexOptions.IgnoreCase), "Me gustaría"),
        (new Regex(@"\bPlease let me know\b", RegexOptions.IgnoreCase), "Por favor, avíseme"),
        (new Regex(@"\bThank you very much\b", RegexOptions.IgnoreCase), "Muchas gracias"),
        (new Regex(@"\bThank you\b", RegexOptions.IgnoreCase), "Gracias"),
        (new Regex(@"\bGood morning\b", RegexOptions.IgnoreCase), "Buenos días"),
        (new Regex(@"\bGood afternoon\b", RegexOptions.IgnoreCase), "Buenas tardes"),
        (new Regex(@"\bGood evening\b", RegexOptions.IgnoreCase), "Buenas noches"),
        (new Regex(@"\bnext Monday\b", RegexOptions.IgnoreCase), "el próximo lunes"),
        (new Regex(@"\bnext Tuesday\b", RegexOptions.IgnoreCase), "el próximo martes"),
        (new Regex(@"\bnext Wednesday\b", RegexOptions.IgnoreCase), "el próximo miércoles"),
        (new Regex(@"\bnext Thursday\b", RegexOptions.IgnoreCase), "el próximo jueves"),
        (new Regex(@"\bnext Friday\b", RegexOptions.IgnoreCase), "el próximo viernes"),
        (new Regex(@"\bwaking up early\b", RegexOptions.IgnoreCase), "madrugar"),
        (new Regex(@"\bget used to\b", RegexOptions.IgnoreCase), "acostumbrarme a"),
        (new Regex(@"\bmeeting\b", RegexOptions.IgnoreCase), "reunión"),
        (new Regex(@"\bschedule\b", RegexOptions.IgnoreCase), "programar"),
        (new Regex(@"\bappointment\b", RegexOptions.IgnoreCase), "cita"),
        (new Regex(@"\btomorrow\b", RegexOptions.IgnoreCase), "mañana"),
        (new Regex(@"\btoday\b", RegexOptions.IgnoreCase), "hoy"),
        (new Regex(@"\bsincerely\b", RegexOptions.IgnoreCase), "Atentamente"),
        (new Regex(@"\bregards\b", RegexOptions.IgnoreCase), "Saludos")
    ];

    public string Name => "Built-in";

    public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();

        var translatedText = TranslateText(request.SourceText);

        stopwatch.Stop();
        return Task.FromResult(new TranslationResult(translatedText, stopwatch.Elapsed, null));
    }

    private static string TranslateText(string sourceText)
    {
        var lines = sourceText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var translatedLines = lines.Select(TranslateLine);
        return string.Join(Environment.NewLine, translatedLines);
    }

    private static string TranslateLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        var leadingWhitespaceLength = line.Length - line.TrimStart().Length;
        var trailingWhitespaceStart = line.TrimEnd().Length;
        var leadingWhitespace = line[..leadingWhitespaceLength];
        var trailingWhitespace = line[trailingWhitespaceStart..];
        var content = line.Trim();

        if (SentenceMap.TryGetValue(content, out var exactTranslation))
        {
            return leadingWhitespace + exactTranslation + trailingWhitespace;
        }

        var translatedContent = content;
        foreach (var (pattern, replacement) in PatternRules)
        {
            translatedContent = pattern.Replace(translatedContent, replacement);
        }

        return leadingWhitespace + translatedContent + trailingWhitespace;
    }
}
