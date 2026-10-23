using System;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>
    /// Plugin settings. Persisted by Flow Launcher as JSON (see <see cref="IPublicAPI.LoadSettingJsonStorage{T}"/>).
    /// </summary>
    public class Settings : BaseModel
    {
        /// <summary>
        /// DeepL API key, encrypted at rest with Windows DPAPI (CurrentUser scope) and stored as Base64.
        /// Empty when no key has been configured.
        /// </summary>
        public string EncryptedApiKey { get; set; } = string.Empty;

        /// <summary>
        /// true = use the DeepL API Free endpoint (api-free.deepl.com); false = Pro endpoint (api.deepl.com).
        /// </summary>
        public bool UseFreeEndpoint { get; set; } = true;

        /// <summary>
        /// Default source language. <see cref="Languages.AutoDetectCode"/> ("auto") means DeepL detects the source.
        /// </summary>
        public string DefaultSourceLanguage { get; set; } = Languages.AutoDetectCode;

        /// <summary>
        /// Default target language (DeepL target code, e.g. "EN-US").
        /// </summary>
        public string DefaultTargetLanguage { get; set; } = "EN-US";

        public const int MinTranslationDelayMs = 200;
        public const int MaxTranslationDelayMs = 5000;
        public const int DefaultTranslationDelayMs = 450;

        private int _translationDelayMs = DefaultTranslationDelayMs;

        /// <summary>
        /// Milliseconds Flow Launcher waits after the user stops typing before querying this plugin.
        /// Clamped to [<see cref="MinTranslationDelayMs"/>, <see cref="MaxTranslationDelayMs"/>].
        /// </summary>
        public int TranslationDelayMs
        {
            get => _translationDelayMs;
            set => _translationDelayMs = Math.Clamp(value, MinTranslationDelayMs, MaxTranslationDelayMs);
        }
    }
}
