using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>
    /// A language entry. <see cref="Code"/> is the DeepL <em>target</em> code,
    /// <see cref="SourceCode"/> is the DeepL <em>source</em> code (regional variants share a base source code).
    /// </summary>
    public sealed record LanguageInfo(
        string Code,
        string Name,
        string SourceCode,
        string SourceName,
        string[] Aliases);

    /// <summary>
    /// Static catalog of DeepL-supported languages and code normalization helpers.
    /// User-typed codes are matched case-insensitively against <see cref="LanguageInfo.Code"/> and
    /// <see cref="LanguageInfo.Aliases"/>.
    /// </summary>
    public static class Languages
    {
        public const string AutoDetectCode = "auto";
        public const string AutoDetectName = "Auto-detect";

        public static readonly LanguageInfo[] All =
        {
            new("EN-US",  "English (US)",        "EN", "English",       new[] { "en", "en-us", "enus", "english" }),
            new("EN-GB",  "English (UK)",        "EN", "English",       new[] { "en-gb", "engb", "english-uk" }),
            new("AR",     "Arabic",              "AR", "Arabic",        new[] { "ar", "arabic", "ara" }),
            new("BG",     "Bulgarian",           "BG", "Bulgarian",     new[] { "bg", "bulgarian", "bul" }),
            new("CS",     "Czech",               "CS", "Czech",         new[] { "cs", "czech", "ces" }),
            new("DA",     "Danish",              "DA", "Danish",        new[] { "da", "danish", "dan" }),
            new("DE",     "German",              "DE", "German",        new[] { "de", "german", "deu", "ger" }),
            new("EL",     "Greek",               "EL", "Greek",         new[] { "el", "greek", "ell", "gre" }),
            new("ES",     "Spanish",             "ES", "Spanish",       new[] { "es", "spanish", "spa" }),
            new("ET",     "Estonian",            "ET", "Estonian",      new[] { "et", "estonian", "est" }),
            new("FI",     "Finnish",             "FI", "Finnish",       new[] { "fi", "finnish", "fin" }),
            new("FR",     "French",              "FR", "French",        new[] { "fr", "french", "fra", "fre" }),
            new("HE",     "Hebrew",              "HE", "Hebrew",        new[] { "he", "hebrew", "heb" }),
            new("HU",     "Hungarian",           "HU", "Hungarian",     new[] { "hu", "hungarian", "hun" }),
            new("ID",     "Indonesian",          "ID", "Indonesian",    new[] { "id", "indonesian", "ind" }),
            new("IT",     "Italian",             "IT", "Italian",       new[] { "it", "italian", "ita" }),
            new("JA",     "Japanese",            "JA", "Japanese",      new[] { "ja", "japanese", "jpn", "jp" }),
            new("KO",     "Korean",              "KO", "Korean",        new[] { "ko", "korean", "kor", "kr" }),
            new("LT",     "Lithuanian",          "LT", "Lithuanian",    new[] { "lt", "lithuanian", "lit" }),
            new("LV",     "Latvian",             "LV", "Latvian",       new[] { "lv", "latvian", "lav" }),
            new("NB",     "Norwegian (Bokmål)",  "NB", "Norwegian",     new[] { "nb", "no", "norwegian", "nob", "nor" }),
            new("NL",     "Dutch",               "NL", "Dutch",         new[] { "nl", "dutch", "nld" }),
            new("PL",     "Polish",              "PL", "Polish",        new[] { "pl", "polish", "pol" }),
            new("PT-BR",  "Portuguese (Brazil)", "PT", "Portuguese",    new[] { "pt", "pt-br", "ptbr", "portuguese", "por", "portuguese-br" }),
            new("PT-PT",  "Portuguese (Portugal)","PT","Portuguese",    new[] { "pt-pt", "ptpt", "portuguese-pt" }),
            new("RO",     "Romanian",            "RO", "Romanian",      new[] { "ro", "romanian", "ron", "rum" }),
            new("RU",     "Russian",             "RU", "Russian",       new[] { "ru", "russian", "rus" }),
            new("SK",     "Slovak",              "SK", "Slovak",        new[] { "sk", "slovak", "slk" }),
            new("SL",     "Slovenian",           "SL", "Slovenian",     new[] { "sl", "slovenian", "slv" }),
            new("SV",     "Swedish",             "SV", "Swedish",       new[] { "sv", "swedish", "swe" }),
            new("TR",     "Turkish",             "TR", "Turkish",       new[] { "tr", "turkish", "tur" }),
            new("UK",     "Ukrainian",           "UK", "Ukrainian",     new[] { "uk", "ukrainian", "ukr" }),
            new("ZH-HANS","Chinese (simplified)","ZH", "Chinese",       new[] { "zh", "zh-hans", "zh-cn", "zhs", "chinese", "simplified" }),
            new("ZH-HANT","Chinese (traditional)","ZH","Chinese",       new[] { "zh-hant", "zh-tw", "zht", "traditional" }),
        };

        /// <summary>All target languages (each entry in <see cref="All"/> is a valid DeepL target).</summary>
        public static IReadOnlyList<LanguageInfo> TargetLanguages => All;

        /// <summary>Distinct DeepL source languages (base codes), for the settings dropdown.</summary>
        public static IReadOnlyList<(string Code, string Name)> SourceLanguages =>
            All
                .GroupBy(x => x.SourceCode)
                .Select(g => (g.Key, g.First().SourceName))
                .OrderBy(x => x.Item2, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>Finds the catalog entry matching a user-typed code (Code or alias, case-insensitive).</summary>
        public static LanguageInfo? Resolve(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var value = input.Trim();
            return All.FirstOrDefault(l =>
                string.Equals(l.Code, value, StringComparison.OrdinalIgnoreCase) ||
                l.Aliases.Any(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>Resolves input to a DeepL target code, or null when unknown.</summary>
        public static string? NormalizeTarget(string? input) => Resolve(input)?.Code;

        /// <summary>Resolves input to a DeepL source code ("auto" resolves to <see cref="AutoDetectCode"/>), or null when unknown.</summary>
        public static string? NormalizeSource(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var value = input.Trim();
            if (value.Equals("auto", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("autodetect", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("detect", StringComparison.OrdinalIgnoreCase))
                return AutoDetectCode;

            return Resolve(value)?.SourceCode;
        }

        /// <summary>True if the input matches any language code or alias (including "auto").</summary>
        public static bool IsKnownCode(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var value = input.Trim();
            if (value.Equals("auto", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("autodetect", StringComparison.OrdinalIgnoreCase))
                return true;

            return Resolve(value) is not null;
        }

        /// <summary>Display name for a target code (falls back to the code itself).</summary>
        public static string GetTargetName(string code) =>
            All.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? code;

        /// <summary>Display name for a source code ("auto" → Auto-detect).</summary>
        public static string GetSourceName(string code)
        {
            if (string.Equals(code, AutoDetectCode, StringComparison.OrdinalIgnoreCase))
                return AutoDetectName;

            return All.FirstOrDefault(x => string.Equals(x.SourceCode, code, StringComparison.OrdinalIgnoreCase))?.SourceName ?? code;
        }
    }
}
