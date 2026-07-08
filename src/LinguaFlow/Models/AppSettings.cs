namespace LinguaFlow.Models;

public sealed class AppSettings
{
    public string TranslationMode { get; set; } = "Live";

    public string TranslationEngine { get; set; } = "Ollama";

    public string OllamaModel { get; set; } = "mistral-nemo:latest";

    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    public bool NotifyWhenOllamaUnavailable { get; set; } = true;

    public int DebounceDelayMilliseconds { get; set; } = 700;

    public string TranslationStyle { get; set; } = "Natural";

    public double FontSize { get; set; } = 15;
}
