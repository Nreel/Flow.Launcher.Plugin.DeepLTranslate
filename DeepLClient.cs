using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>Result of a successful DeepL translation.</summary>
    public sealed record TranslationResult(string Text, string? DetectedSourceLanguage);

    /// <summary>Raised when the DeepL API returns an error or the response cannot be parsed.</summary>
    public sealed class DeepLException : Exception
    {
        public DeepLException(string message) : base(message) { }
    }

    /// <summary>
    /// Thin HTTP client for the DeepL text-translation endpoint.
    /// </summary>
    public sealed class DeepLClient
    {
        private const string FreeEndpoint = "https://api-free.deepl.com/v2/translate";
        private const string ProEndpoint = "https://api.deepl.com/v2/translate";

        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Translates <paramref name="text"/> from <paramref name="sourceLanguage"/> to <paramref name="targetLanguage"/>.
        /// A null/"auto" <paramref name="sourceLanguage"/> lets DeepL auto-detect the source language.
        /// </summary>
        public async Task<TranslationResult> TranslateAsync(
            string text,
            string? sourceLanguage,
            string targetLanguage,
            string apiKey,
            bool useFreeEndpoint,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Nothing to translate.", nameof(text));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("DeepL API key is not configured.", nameof(apiKey));

            var endpoint = useFreeEndpoint ? FreeEndpoint : ProEndpoint;

            var payload = new Dictionary<string, object>
            {
                ["text"] = new[] { text },
                ["target_lang"] = targetLanguage
            };

            if (!string.IsNullOrWhiteSpace(sourceLanguage) &&
                !string.Equals(sourceLanguage, Languages.AutoDetectCode, StringComparison.OrdinalIgnoreCase))
            {
                payload["source_lang"] = sourceLanguage;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await Http.SendAsync(request, token).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new DeepLException(ParseErrorMessage(body, response.StatusCode));

            DeepLTranslateResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<DeepLTranslateResponse>(body);
            }
            catch (JsonException)
            {
                throw new DeepLException("Unexpected response from DeepL.");
            }

            var translation = parsed?.Translations is { Count: > 0 } ? parsed.Translations[0] : null;
            if (translation?.Text is null)
                throw new DeepLException("DeepL returned an empty translation.");

            return new TranslationResult(translation.Text, translation.DetectedSourceLanguage);
        }

        private static string ParseErrorMessage(string body, HttpStatusCode status)
        {
            try
            {
                var error = JsonSerializer.Deserialize<DeepLErrorResponse>(body);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                    return error.Message;
            }
            catch (JsonException)
            {
                // Fall through to a generic message.
            }

            return $"DeepL API error ({(int)status}).";
        }

        private sealed class DeepLTranslateResponse
        {
            [JsonPropertyName("translations")]
            public List<DeepLTranslation>? Translations { get; set; }
        }

        private sealed class DeepLTranslation
        {
            [JsonPropertyName("detected_source_language")]
            public string? DetectedSourceLanguage { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private sealed class DeepLErrorResponse
        {
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
