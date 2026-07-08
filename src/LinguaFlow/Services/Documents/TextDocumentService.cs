namespace LinguaFlow.Services.Documents;

using System.IO;
using System.Text;

public sealed class TextDocumentService
{
    public async Task<string> OpenAsync(string path, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
    }

    public async Task SaveAsync(string path, string sourceText, string translatedText, CancellationToken cancellationToken)
    {
        var output = string.IsNullOrWhiteSpace(translatedText)
            ? sourceText
            : $"""
              English
              -------
              {sourceText}

              Spanish
              -------
              {translatedText}
              """;

        await File.WriteAllTextAsync(path, output, Encoding.UTF8, cancellationToken);
    }
}
