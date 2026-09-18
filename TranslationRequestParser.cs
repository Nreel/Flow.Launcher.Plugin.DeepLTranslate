using System;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>A parsed translation request resolved from a user query.</summary>
    /// <param name="SourceLanguage">DeepL source code, or "auto"/null for auto-detect.</param>
    /// <param name="TargetLanguage">DeepL target code.</param>
    /// <param name="Text">Text to translate (may be empty when only a language override was typed).</param>
    public sealed record TranslationRequest(string? SourceLanguage, string TargetLanguage, string Text);

    /// <summary>
    /// Parses the text after the trigger phrase into source/target/text.
    /// Rules:
    /// <list type="bullet">
    ///   <item><c>tr hello</c> → default source + default target, text "hello".</item>
    ///   <item><c>tr es hola</c> → target override to Spanish, source auto-detect, text "hola".</item>
    ///   <item><c>tr en es hello</c> → source English, target Spanish, text "hello".</item>
    /// </list>
    /// </summary>
    public static class TranslationRequestParser
    {
        public static TranslationRequest Parse(string? search, Settings settings)
        {
            var terms = (search ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            string? source = settings.DefaultSourceLanguage;
            string target = settings.DefaultTargetLanguage;
            int textStart = 0;

            if (terms.Length > 0 && Languages.IsKnownCode(terms[0]))
            {
                var firstCode = terms[0];

                if (terms.Length >= 2 && Languages.IsKnownCode(terms[1]))
                {
                    // Two leading codes → source + target override.
                    source = Languages.NormalizeSource(firstCode) ?? source;
                    target = Languages.NormalizeTarget(terms[1]) ?? target;
                    textStart = 2;
                }
                else
                {
                    // Single leading code → target override, source stays auto-detect.
                    source = Languages.AutoDetectCode;
                    target = Languages.NormalizeTarget(firstCode) ?? target;
                    textStart = 1;
                }
            }

            var text = textStart < terms.Length
                ? string.Join(' ', terms[textStart..])
                : string.Empty;

            return new TranslationRequest(source, target, text);
        }
    }
}
