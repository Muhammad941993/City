using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CityGuide.Web.Services;

public class TurnstileOptions
{
    public string? SiteKey { get; set; }
    public string? SecretKey { get; set; }
    public bool Enabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);
}

/// <summary>
/// Cloudflare Turnstile spam check for the public forms.
/// When no keys are configured (local development) every request passes.
/// </summary>
public class Turnstile(HttpClient http, IOptions<TurnstileOptions> options, ILogger<Turnstile> logger)
{
    public TurnstileOptions Options => options.Value;

    public async Task<bool> VerifyAsync(HttpContext ctx)
    {
        if (!Options.Enabled)
            return true;

        var form = ctx.Request.HasFormContentType ? await ctx.Request.ReadFormAsync() : null;
        var token = form?["cf-turnstile-response"].ToString();
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            using var resp = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = Options.SecretKey!,
                    ["response"] = token,
                    ["remoteip"] = ctx.Connection.RemoteIpAddress?.ToString() ?? "",
                }));
            var result = await resp.Content.ReadFromJsonAsync<Result>();
            return result?.Success == true;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Turnstile verification failed");
            return false;
        }
    }

    private sealed class Result
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
