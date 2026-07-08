namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LinguaFlow.Helpers;
using LinguaFlow.Models;
using LinguaFlow.Services.Ollama;
using LinguaFlow.Services.Settings;
using LinguaFlow.Services.Translation;

public sealed class MainViewModel : ObservableObject
{
    private readonly OllamaClient ollamaClient;
    private readonly AppSettingsService settingsService;
    private readonly IReadOnlyDictionary<string, ITranslationService> translationServices;
    private CancellationTokenSource? debounceCancellation;
    private CancellationTokenSource? translationCancellation;
    private AppSettings settings;
    private string sourceText = string.Empty;
    private string translatedText = string.Empty;
    private string selectedModel = "mistral-nemo:latest";
    private string selectedTranslationMode = "Live";
    private string selectedTranslationEngine = "Ollama";
    private string translationStatus = "Ready";
    private string latencyDisplay = "Latency: --";
    private string tokenUsageDisplay = "Tokens: --";
    private string translationStyle = "Natural";
    private int debounceDelayMilliseconds = 700;
    private double editorFontSize = 15;
    private bool notifyWhenOllamaUnavailable = true;
    private int wordCount;
    private int characterCount;

    public MainViewModel()
    {
        settingsService = new AppSettingsService();
        settings = settingsService.Load();
        ollamaClient = new OllamaClient(settings.OllamaEndpoint);
        translationServices = new Dictionary<string, ITranslationService>
        {
            ["Built-in"] = new BuiltInTranslationService(),
            ["Ollama"] = new OllamaTranslationService(ollamaClient)
        };

        TranslationEngines = new ObservableCollection<string>(translationServices.Keys);
        TranslationModes = new ObservableCollection<string>
        {
            "Live",
            "Manual"
        };

        ApplySettings(settings, save: false, queueTranslation: false);

        AvailableModels = new ObservableCollection<string> { SelectedModel };

        NewDocumentCommand = new RelayCommand(_ => NewDocument());
        OpenDocumentCommand = CreatePlaceholderCommand("Open document is not connected yet.");
        SaveDocumentCommand = CreatePlaceholderCommand("Save document is not connected yet.");
        CopyTranslationCommand = new RelayCommand(_ => CopyTranslation(), _ => !string.IsNullOrWhiteSpace(TranslatedText));
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, CanTranslate);

        _ = LoadOllamaModelsAsync();
    }

    public ObservableCollection<string> TranslationEngines { get; }

    public ObservableCollection<string> TranslationModes { get; }

    public ObservableCollection<string> AvailableModels { get; }

    public string SourceText
    {
        get => sourceText;
        set
        {
            if (SetProperty(ref sourceText, value))
            {
                UpdateDocumentCounts();
                RaiseTranslationCommandState();
                QueueAutomaticTranslationIfLive();
            }
        }
    }

    public string TranslatedText
    {
        get => translatedText;
        set
        {
            if (SetProperty(ref translatedText, value))
            {
                RaiseCopyCommandState();
            }
        }
    }

    public string SelectedTranslationEngine
    {
        get => selectedTranslationEngine;
        set
        {
            if (SetProperty(ref selectedTranslationEngine, value))
            {
                OnPropertyChanged(nameof(IsOllamaSelected));
                TranslationStatus = value == "Ollama" ? "Ollama translation selected." : "Built-in translation selected.";
                QueueAutomaticTranslationIfLive();
            }
        }
    }

    public string SelectedTranslationMode
    {
        get => selectedTranslationMode;
        set
        {
            if (SetProperty(ref selectedTranslationMode, value))
            {
                OnPropertyChanged(nameof(IsLiveTranslationEnabled));
                OnPropertyChanged(nameof(ManualTranslateButtonVisibility));
                TranslationStatus = IsLiveTranslationEnabled ? "Live sync enabled." : "Manual translation enabled.";
                QueueAutomaticTranslationIfLive();
            }
        }
    }

    public bool IsLiveTranslationEnabled => SelectedTranslationMode == "Live";

    public Visibility ManualTranslateButtonVisibility => IsLiveTranslationEnabled ? Visibility.Collapsed : Visibility.Visible;

    public bool IsOllamaSelected => SelectedTranslationEngine == "Ollama";

    public string SelectedModel
    {
        get => selectedModel;
        set
        {
            if (SetProperty(ref selectedModel, value))
            {
                QueueAutomaticTranslationIfLive();
            }
        }
    }

    public string TranslationStatus
    {
        get => translationStatus;
        set => SetProperty(ref translationStatus, value);
    }

    public string LatencyDisplay
    {
        get => latencyDisplay;
        set => SetProperty(ref latencyDisplay, value);
    }

    public int WordCount
    {
        get => wordCount;
        set => SetProperty(ref wordCount, value);
    }

    public int CharacterCount
    {
        get => characterCount;
        set => SetProperty(ref characterCount, value);
    }

    public string TokenUsageDisplay
    {
        get => tokenUsageDisplay;
        set => SetProperty(ref tokenUsageDisplay, value);
    }

    public double EditorFontSize
    {
        get => editorFontSize;
        set => SetProperty(ref editorFontSize, value);
    }

    public ICommand NewDocumentCommand { get; }

    public ICommand OpenDocumentCommand { get; }

    public ICommand SaveDocumentCommand { get; }

    public ICommand CopyTranslationCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand TranslateCommand { get; }

    private async Task LoadOllamaModelsAsync()
    {
        try
        {
            var models = await ollamaClient.GetModelsAsync(CancellationToken.None);
            if (models.Count == 0)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableModels.Clear();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }

                if (AvailableModels.Contains("mistral-nemo:latest"))
                {
                    SelectedModel = "mistral-nemo:latest";
                }
                else
                {
                    SelectedModel = AvailableModels[0];
                }

                SelectedTranslationEngine = "Ollama";
                TranslationStatus = "Ollama models detected.";
            });
        }
        catch
        {
            TranslationStatus = notifyWhenOllamaUnavailable
                ? "Ollama is not running. Start Ollama or switch to Built-in."
                : "Built-in translation is available.";
        }
    }

    private async Task TranslateAsync()
    {
        await TranslateCurrentTextAsync();
    }

    private async Task TranslateCurrentTextAsync()
    {
        translationCancellation?.Cancel();
        translationCancellation = new CancellationTokenSource();

        try
        {
            TranslationStatus = "Translating...";
            var service = translationServices[SelectedTranslationEngine];
            var request = new TranslationRequest(SourceText, "Spanish", translationStyle, SelectedModel);
            var result = await service.TranslateAsync(request, translationCancellation.Token);

            TranslatedText = result.Text;
            LatencyDisplay = $"Latency: {result.Latency.TotalMilliseconds:0} ms";
            TokenUsageDisplay = result.TokenUsage is null ? "Tokens: --" : $"Tokens: {result.TokenUsage}";
            TranslationStatus = "Translation complete.";
        }
        catch (OperationCanceledException)
        {
            TranslationStatus = "Translation canceled.";
        }
        catch (Exception exception)
        {
            TranslationStatus = $"Translation failed: {exception.Message}";
        }
    }

    private void QueueAutomaticTranslationIfLive()
    {
        if (IsLiveTranslationEnabled)
        {
            QueueAutomaticTranslation();
        }
    }

    private void QueueAutomaticTranslation()
    {
        debounceCancellation?.Cancel();

        if (string.IsNullOrWhiteSpace(SourceText))
        {
            translationCancellation?.Cancel();
            TranslatedText = string.Empty;
            TranslationStatus = "Ready";
            LatencyDisplay = "Latency: --";
            TokenUsageDisplay = "Tokens: --";
            return;
        }

        debounceCancellation = new CancellationTokenSource();
        var cancellationToken = debounceCancellation.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(debounceDelayMilliseconds), cancellationToken);
                var translateTask = await Application.Current.Dispatcher.InvokeAsync(() =>
                    cancellationToken.IsCancellationRequested ? Task.CompletedTask : TranslateCurrentTextAsync());

                await translateTask;
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private bool CanTranslate()
    {
        return !string.IsNullOrWhiteSpace(SourceText);
    }

    private void NewDocument()
    {
        SourceText = string.Empty;
        TranslatedText = string.Empty;
        LatencyDisplay = "Latency: --";
        TokenUsageDisplay = "Tokens: --";
        TranslationStatus = "New document ready.";
    }

    private void CopyTranslation()
    {
        if (!string.IsNullOrWhiteSpace(TranslatedText))
        {
            Clipboard.SetText(TranslatedText);
            TranslationStatus = "Translation copied.";
        }
    }

    private void OpenSettings()
    {
        try
        {
            var settingsViewModel = new SettingsViewModel(settings, AvailableModels);
            var settingsWindow = new SettingsWindow(settingsViewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (settingsWindow.ShowDialog() == true)
            {
                ApplySettings(settingsViewModel.ToSettings(), save: true, queueTranslation: true);
                TranslationStatus = "Settings saved.";
            }
        }
        catch (Exception exception)
        {
            TranslationStatus = $"Settings could not be saved: {exception.Message}";
        }
    }

    private void ApplySettings(AppSettings newSettings, bool save, bool queueTranslation)
    {
        settings = newSettings;
        debounceDelayMilliseconds = Math.Clamp(settings.DebounceDelayMilliseconds, 250, 5000);
        translationStyle = settings.TranslationStyle;
        notifyWhenOllamaUnavailable = settings.NotifyWhenOllamaUnavailable;
        EditorFontSize = settings.FontSize;
        ollamaClient.ConfigureEndpoint(settings.OllamaEndpoint);

        selectedTranslationMode = settings.TranslationMode;
        selectedTranslationEngine = settings.TranslationEngine;
        selectedModel = settings.OllamaModel;
        OnPropertyChanged(nameof(SelectedTranslationMode));
        OnPropertyChanged(nameof(SelectedTranslationEngine));
        OnPropertyChanged(nameof(SelectedModel));
        OnPropertyChanged(nameof(IsLiveTranslationEnabled));
        OnPropertyChanged(nameof(IsOllamaSelected));
        OnPropertyChanged(nameof(ManualTranslateButtonVisibility));

        if (save)
        {
            settingsService.Save(settings);
        }

        if (queueTranslation)
        {
            QueueAutomaticTranslationIfLive();
        }
    }

    private void UpdateDocumentCounts()
    {
        CharacterCount = SourceText.Length;
        WordCount = SourceText.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private RelayCommand CreatePlaceholderCommand(string statusMessage)
    {
        return new RelayCommand(_ => TranslationStatus = statusMessage);
    }

    private void RaiseTranslationCommandState()
    {
        if (TranslateCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private void RaiseCopyCommandState()
    {
        if (CopyTranslationCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
