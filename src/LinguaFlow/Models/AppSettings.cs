namespace LinguaFlow.Models;

/// <summary>
/// User-configurable application preferences persisted between sessions.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets whether translation runs while typing or only when requested.
    /// </summary>
    public string TranslationMode { get; set; } = "Live";

    /// <summary>
    /// Gets or sets the selected translation engine.
    /// </summary>
    public string TranslationEngine { get; set; } = "Ollama";

    /// <summary>
    /// Gets or sets the selected Ollama model.
    /// </summary>
    public string OllamaModel { get; set; } = "mistral-nemo:latest";

    /// <summary>
    /// Gets or sets the Ollama server endpoint.
    /// </summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets whether the app should show a reminder when Ollama is unavailable.
    /// </summary>
    public bool NotifyWhenOllamaUnavailable { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay after typing stops before live translation begins.
    /// </summary>
    public int DebounceDelayMilliseconds { get; set; } = 700;

    /// <summary>
    /// Gets or sets the requested translation style.
    /// </summary>
    public string TranslationStyle { get; set; } = "Natural";

    /// <summary>
    /// Gets or sets the document editor font size.
    /// </summary>
    public double FontSize { get; set; } = 15;
}
