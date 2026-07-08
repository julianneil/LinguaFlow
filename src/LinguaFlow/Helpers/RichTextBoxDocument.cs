namespace LinguaFlow.Helpers;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

public static class RichTextBoxDocument
{
    // RichTextBox does not expose a bindable Text property. This attached-property shim
    // keeps plain text in MVVM for now; formatted document syncing will need a different path.
    public static readonly DependencyProperty BindPlainTextProperty = DependencyProperty.RegisterAttached(
        "BindPlainText",
        typeof(bool),
        typeof(RichTextBoxDocument),
        new PropertyMetadata(false, OnBindPlainTextChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(RichTextBoxDocument),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static bool GetBindPlainText(DependencyObject element)
    {
        return (bool)element.GetValue(BindPlainTextProperty);
    }

    public static void SetBindPlainText(DependencyObject element, bool value)
    {
        element.SetValue(BindPlainTextProperty, value);
    }

    public static string GetText(DependencyObject element)
    {
        return (string)element.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject element, string value)
    {
        element.SetValue(TextProperty, value);
    }

    private static void OnBindPlainTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not RichTextBox richTextBox)
        {
            return;
        }

        richTextBox.Loaded -= OnRichTextBoxLoaded;
        richTextBox.TextChanged -= OnRichTextBoxTextChanged;

        if (args.NewValue is true)
        {
            richTextBox.Loaded += OnRichTextBoxLoaded;
            richTextBox.TextChanged += OnRichTextBoxTextChanged;
        }
    }

    private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not RichTextBox richTextBox)
        {
            return;
        }

        var newText = args.NewValue as string ?? string.Empty;
        var currentText = GetDocumentText(richTextBox);
        if (currentText == newText)
        {
            return;
        }

        // Updating the document raises TextChanged. Unhook first or this quietly loops back
        // through the binding system and makes typing feel cursed.
        richTextBox.TextChanged -= OnRichTextBoxTextChanged;
        SetDocumentText(richTextBox, newText);
        richTextBox.TextChanged += OnRichTextBoxTextChanged;
    }

    private static void OnRichTextBoxLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is RichTextBox richTextBox)
        {
            SetDocumentText(richTextBox, GetText(richTextBox));
        }
    }

    private static void OnRichTextBoxTextChanged(object sender, TextChangedEventArgs args)
    {
        if (sender is not RichTextBox richTextBox)
        {
            return;
        }

        // SetCurrentValue preserves the binding expression. SetValue looks tempting here,
        // but it can replace the binding and make the editor stop reporting changes.
        richTextBox.SetCurrentValue(TextProperty, GetDocumentText(richTextBox));
        richTextBox.GetBindingExpression(TextProperty)?.UpdateSource();
    }

    private static string GetDocumentText(RichTextBox richTextBox)
    {
        var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }

    private static void SetDocumentText(RichTextBox richTextBox, string text)
    {
        richTextBox.Document.Blocks.Clear();
        richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
    }
}
