namespace LinguaFlow.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LinguaFlow.Helpers;
using LinguaFlow.Models;
using LinguaFlow.Services.Documents;
using LinguaFlow.Services.Ollama;
using LinguaFlow.Services.Settings;
using LinguaFlow.Services.Translation;
using Microsoft.Win32;

public sealed class MainViewModel : ObservableObject
{
    private readonly OllamaClient ollamaClient;
    private readonly AppSettingsService settingsService;
    private readonly TextDocumentService textDocumentService;
    private readonly IReadOnlyDictionary<string, ITranslationService> translationServices;
    private CancellationTokenSource? debounceCancellation;
    private CancellationTokenSource? translationCancellation;
    private AppSettings settings;
    private string sourceText = string.Empty;
    private string translatedText = string.Empty;
    private string? currentFilePath;
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
    private bool hasUnsavedChanges;
    private int wordCount;
    private int characterCount;

    public MainViewModel()
    {
        settingsService = new AppSettingsService();
        textDocumentService = new TextDocumentService();
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
        OpenDocumentCommand = new AsyncRelayCommand(OpenDocumentAsync);
        SaveDocumentCommand = new AsyncRelayCommand(SaveDocumentAsync);
        SaveDocumentAsCommand = new AsyncRelayCommand(SaveDocumentAsAsync);
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
                MarkDocumentChanged();
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

    public string WindowTitle
    {
        get
        {
            var fileName = currentFilePath is null ? "Untitled" : Path.GetFileName(currentFilePath);
            var marker = hasUnsavedChanges ? "*" : string.Empty;
            return $"{fileName}{marker} - LinguaFlow";
        }
    }

    public string DocumentStatus => currentFilePath is null ? "Unsaved document" : currentFilePath;

    public ICommand NewDocumentCommand { get; }

    public ICommand OpenDocumentCommand { get; }

    public ICommand SaveDocumentCommand { get; }

    public ICommand SaveDocumentAsCommand { get; }

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

        // The app intentionally lets old delayed tasks die quietly. Only the newest pause in
        // typing should reach Ollama; otherwise slow responses can overwrite fresher text.
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
        translationCancellation?.Cancel();
        debounceCancellation?.Cancel();
        currentFilePath = null;
        SourceText = string.Empty;
        TranslatedText = string.Empty;
        SetUnsavedChanges(false);
        LatencyDisplay = "Latency: --";
        TokenUsageDisplay = "Tokens: --";
        TranslationStatus = "New document ready.";
    }

    private async Task OpenDocumentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Open English Text"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var text = await textDocumentService.OpenAsync(dialog.FileName, CancellationToken.None);
            currentFilePath = dialog.FileName;
            SourceText = text;
            TranslatedText = string.Empty;
            SetUnsavedChanges(false);
            TranslationStatus = "Document opened.";
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DocumentStatus));
        }
        catch (Exception exception)
        {
            TranslationStatus = $"Open failed: {exception.Message}";
        }
    }

    private async Task SaveDocumentAsync()
    {
        var path = currentFilePath;
        if (path is null)
        {
            path = GetSavePath("Untitled.txt");
            if (path is null)
            {
                return;
            }
        }

        await SaveDocumentToPathAsync(path);
    }

    private async Task SaveDocumentAsAsync()
    {
        var path = GetSavePath(currentFilePath is null ? "Untitled.txt" : Path.GetFileName(currentFilePath));
        if (path is not null)
        {
            await SaveDocumentToPathAsync(path);
        }
    }

    private async Task SaveDocumentToPathAsync(string path)
    {
        try
        {
            await textDocumentService.SaveAsync(path, SourceText, TranslatedText, CancellationToken.None);
            currentFilePath = path;
            SetUnsavedChanges(false);
            TranslationStatus = "Document saved.";
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DocumentStatus));
        }
        catch (Exception exception)
        {
            TranslationStatus = $"Save failed: {exception.Message}";
        }
    }

    private static string? GetSavePath(string fileName)
    {
        var dialog = new SaveFileDialog
        {
            DefaultExt = ".txt",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = fileName,
            Title = "Save LinguaFlow Text"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
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

        // Assign the backing fields directly so loading saved settings does not trigger a
        // translation request before the rest of the window has caught up.
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

    private void MarkDocumentChanged()
    {
        if (!hasUnsavedChanges)
        {
            SetUnsavedChanges(true);
        }
    }

    private void SetUnsavedChanges(bool value)
    {
        if (hasUnsavedChanges == value)
        {
            return;
        }

        hasUnsavedChanges = value;
        OnPropertyChanged(nameof(WindowTitle));
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
