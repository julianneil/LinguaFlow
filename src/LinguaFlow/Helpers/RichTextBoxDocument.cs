namespace LinguaFlow.Helpers;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

/// <summary>
/// Provides simple text binding for RichTextBox until full formatted document binding is added.
/// </summary>
public static class RichTextBoxDocument
{
    /// <summary>
    /// Dependency property used to bind RichTextBox plain text to a view model property.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(RichTextBoxDocument),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    /// <summary>
    /// Reads the bound text value.
    /// </summary>
    /// <param name="element">The target RichTextBox.</param>
    /// <returns>The bound text value.</returns>
    public static string GetText(DependencyObject element)
    {
        return (string)element.GetValue(TextProperty);
    }

    /// <summary>
    /// Updates the bound text value.
    /// </summary>
    /// <param name="element">The target RichTextBox.</param>
    /// <param name="value">The text to place in the editor.</param>
    public static void SetText(DependencyObject element, string value)
    {
        element.SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not RichTextBox richTextBox)
        {
            return;
        }

        richTextBox.TextChanged -= OnRichTextBoxTextChanged;

        var newText = args.NewValue as string ?? string.Empty;
        var currentText = GetDocumentText(richTextBox);
        if (currentText != newText)
        {
            SetDocumentText(richTextBox, newText);
        }

        richTextBox.TextChanged += OnRichTextBoxTextChanged;
    }

    private static void OnRichTextBoxTextChanged(object sender, TextChangedEventArgs args)
    {
        if (sender is RichTextBox richTextBox)
        {
            richTextBox.SetCurrentValue(TextProperty, GetDocumentText(richTextBox));
            richTextBox.GetBindingExpression(TextProperty)?.UpdateSource();
        }
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
