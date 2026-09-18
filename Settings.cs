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
    }
}
