# LinguaFlow

LinguaFlow is a Windows desktop writing application for creating polished bilingual documents. The first target workflow is English to Spanish: users write naturally in English while a local AI model produces a fluent Spanish version that reads as if it were originally written by a native speaker.

The goal is not word-for-word translation. LinguaFlow is designed to preserve meaning, tone, paragraph structure, headings, lists, and professional formatting while adapting the output into natural Spanish.

## Vision

LinguaFlow should feel closer to a document editor than a translation utility. The user writes in an English rich text editor, and the translated document updates alongside it. The finished application is intended to support a professional desktop workflow with file operations, export options, translation styles, status indicators, and offline AI through Ollama.

## Planned Features

- Native Windows desktop experience built with WPF
- Rich text English editor
- Read-only Spanish translation pane
- Local AI translation through Ollama
- Model selection, starting with `mistral-nemo:latest`
- Translation styles such as natural, professional, business, legal, medical, academic, casual, and literal
- Debounced real-time translation while typing
- Incremental paragraph-level translation
- Document save/open support
- DOCX and PDF export
- Status bar with model, translation state, latency, word count, character count, and token usage
- Modular MVVM architecture

## Technology Stack

- C#
- .NET
- WPF
- MVVM
- Ollama
- HTTP REST API
- System.Text.Json

## Development Roadmap

### Phase 1: Application Shell

- Create the WPF solution
- Configure the MVVM structure
- Build the main window
- Add the rich text editor layout
- Add the translation pane layout
- Add a dark visual theme

### Phase 2: Ollama Integration

- Connect to the local Ollama API
- Detect installed models
- Allow model selection
- Send translation requests
- Display translated output

### Phase 3: Real-Time Translation

- Debounce editor changes
- Detect changed paragraphs
- Translate only changed content
- Preserve the rest of the document
- Show translation progress and errors

### Phase 4: Documents and Settings

- Add open/save support
- Add DOCX support
- Add PDF export
- Add settings for AI, editor, translation style, and output language

### Phase 5: Polish and Performance

- Add streaming responses
- Improve cancellation behavior
- Add paragraph caching
- Improve error handling
- Refine the user interface

## Ollama Setup

LinguaFlow is designed to use Ollama for local, offline AI translation. Ollama integration will be added in Phase 2, but the expected setup is:

1. Install Ollama from the official site:

   ```text
   https://ollama.com
   ```

2. Pull the recommended local model:

   ```powershell
   ollama pull mistral-nemo:latest
   ```

3. Verify the model is installed:

   ```powershell
   ollama list
   ```

4. Confirm Ollama is running locally:

   ```text
   http://localhost:11434
   ```

The application will use the Ollama chat API endpoint:

```text
http://localhost:11434/api/chat
```

The default model for early development is:

```text
mistral-nemo:latest
```

## Repository Status

This project is in early development. The current focus is building the application foundation one phase at a time, with clean architecture and production-quality code instead of a throwaway prototype.

## License

License information has not been added yet.
