using System.Globalization;
using System.Text.Json;

namespace PowerRealms.Api.Services;

public interface ILocalizationService
{
    string GetMessage(string key, string? culture = null);
    string CurrentCulture { get; }
    void SetCulture(string culture);
    IEnumerable<string> SupportedCultures { get; }
}

public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, object>> _resources = new();
    private string _currentCulture = "en";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        LoadResources();
    }

    public IEnumerable<string> SupportedCultures => new[] { "en", "ru" };

    public string CurrentCulture
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var langHeader = httpContext.Request.Headers["Accept-Language"].FirstOrDefault();
                if (!string.IsNullOrEmpty(langHeader))
                {
                    var lang = langHeader.Split(',').FirstOrDefault()?.Split('-').FirstOrDefault()?.ToLower();
                    if (lang != null && SupportedCultures.Contains(lang))
                        return lang;
                }
                
                var queryLang = httpContext.Request.Query["lang"].FirstOrDefault()?.ToLower();
                if (queryLang != null && SupportedCultures.Contains(queryLang))
                    return queryLang;
            }
            return _currentCulture;
        }
    }

    public void SetCulture(string culture)
    {
        if (SupportedCultures.Contains(culture.ToLower()))
            _currentCulture = culture.ToLower();
    }

    private void LoadResources()
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        
        foreach (var culture in SupportedCultures)
        {
            var filePath = Path.Combine(basePath, $"Messages.{culture}.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (dict != null)
                    _resources[culture] = dict;
            }
        }
    }

    public string GetMessage(string key, string? culture = null)
    {
        var lang = culture?.ToLower() ?? CurrentCulture;
        if (!_resources.ContainsKey(lang))
            lang = "en";

        var parts = key.Split('.');
        if (parts.Length < 2) return key;

        try
        {
            if (_resources.TryGetValue(lang, out var langDict))
            {
                if (langDict.TryGetValue(parts[0], out var category))
                {
                    if (category is JsonElement element)
                    {
                        var current = element;
                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (current.TryGetProperty(parts[i], out var next))
                            {
                                current = next;
                            }
                            else
                            {
                                return key;
                            }
                        }
                        return current.GetString() ?? key;
                    }
                }
            }
        }
        catch
        {
            return key;
        }

        return key;
    }
}
