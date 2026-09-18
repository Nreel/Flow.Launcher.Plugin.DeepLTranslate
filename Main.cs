using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    public class Main : IAsyncPlugin, ISettingProvider
    {
        private PluginInitContext? _context;
        private Settings _settings = new();
        private readonly DeepLClient _client = new();

        private string _iconPath = string.Empty;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
            _iconPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "deepl.png");
            return Task.CompletedTask;
        }

        public Control CreateSettingPanel() => new SettingsView(_context!, _settings);

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();
            var search = (query.Search ?? string.Empty).Trim();

            // Decrypt on demand so a key set in the settings panel is picked up immediately.
            var apiKey = CredentialProtector.Unprotect(_settings.EncryptedApiKey);

            // 1. No API key configured → guide the user to settings.
            if (string.IsNullOrEmpty(apiKey))
            {
                results.Add(BuildMessage(
                    "DeepL API key not configured",
                    "Press Enter to open Flow Launcher settings and add your DeepL API key",
                    _ =>
                    {
                        _context?.API.OpenSettingDialog();
                        return true;
                    }));
                return results;
            }

            // 2. Empty input → show usage helper.
            if (string.IsNullOrEmpty(search))
            {
                results.Add(BuildMessage(
                    "DeepL Translate",
                    "Type text to translate. Override target: \"tr <lang> <text>\" · override both: \"tr <from> <to> <text>\""));
                return results;
            }

            var request = TranslationRequestParser.Parse(search, _settings);

            // 3. Only a language override was typed, no text.
            if (string.IsNullOrEmpty(request.Text))
            {
                var targetName = Languages.GetTargetName(request.TargetLanguage);
                results.Add(BuildMessage(
                    $"Translate to {targetName}",
                    "Type the text to translate after the language code"));
                return results;
            }

            // 4. Translate.
            try
            {
                var translated = await _client.TranslateAsync(
                    request.Text,
                    request.SourceLanguage,
                    request.TargetLanguage,
                    apiKey,
                    _settings.UseFreeEndpoint,
                    token).ConfigureAwait(false);

                var sourceCode = request.SourceLanguage;
                if (string.IsNullOrEmpty(sourceCode) ||
                    string.Equals(sourceCode, Languages.AutoDetectCode, StringComparison.OrdinalIgnoreCase))
                {
                    sourceCode = translated.DetectedSourceLanguage ?? Languages.AutoDetectCode;
                }

                var sourceName = Languages.GetSourceName(sourceCode);
                var targetName = Languages.GetTargetName(request.TargetLanguage);
                var translationText = translated.Text;

                results.Add(new Result
                {
                    Title = translationText,
                    SubTitle = $"{sourceName} → {targetName}",
                    IcoPath = _iconPath,
                    Action = _ =>
                    {
                        _context?.API.CopyToClipboard(translationText);
                        return true;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Query superseded by newer input — return nothing.
            }
            catch (DeepLException ex)
            {
                results.Add(BuildMessage("Translation failed", ex.Message));
            }
            catch (Exception)
            {
                results.Add(BuildMessage("Translation failed", "Could not reach DeepL. Check your internet connection."));
            }

            return results;
        }

        private Result BuildMessage(string title, string subtitle, Func<ActionContext, bool>? action = null) => new()
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = _iconPath,
            Action = action
        };
    }
}
