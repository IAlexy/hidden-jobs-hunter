using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;

    public SettingsController(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _db.Settings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings { Id = 1 };
            _db.Settings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            settings.Id,
            settings.AiProvider,
            HasGeminiKey = !string.IsNullOrEmpty(settings.GeminiApiKey),
            GeminiApiKey = MaskKey(settings.GeminiApiKey),
            settings.GeminiModel,
            HasOpenAiKey = !string.IsNullOrEmpty(settings.OpenAiApiKey),
            OpenAiApiKey = MaskKey(settings.OpenAiApiKey),
            settings.OpenAiModel,
            settings.OllamaEndpoint,
            settings.OllamaModel,
            settings.AutoMatchOnSync,
            settings.OnlyFranceCompatibleJobs,
            settings.UpdatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
    {
        var settings = await _db.Settings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings { Id = 1 };
            _db.Settings.Add(settings);
        }

        if (!string.IsNullOrEmpty(dto.AiProvider))
        {
            settings.AiProvider = dto.AiProvider;
        }

        if (dto.GeminiApiKey != null)
        {
            if (string.IsNullOrEmpty(dto.GeminiApiKey))
            {
                settings.GeminiApiKey = null;
            }
            else if (!IsMaskedKey(dto.GeminiApiKey))
            {
                settings.GeminiApiKey = dto.GeminiApiKey.Trim();
            }
        }

        if (dto.OpenAiApiKey != null)
        {
            if (string.IsNullOrEmpty(dto.OpenAiApiKey))
            {
                settings.OpenAiApiKey = null;
            }
            else if (!IsMaskedKey(dto.OpenAiApiKey))
            {
                settings.OpenAiApiKey = dto.OpenAiApiKey.Trim();
            }
        }

        if (!string.IsNullOrEmpty(dto.GeminiModel)) settings.GeminiModel = dto.GeminiModel;
        if (!string.IsNullOrEmpty(dto.OpenAiModel)) settings.OpenAiModel = dto.OpenAiModel;
        if (!string.IsNullOrEmpty(dto.OllamaEndpoint)) settings.OllamaEndpoint = dto.OllamaEndpoint;
        if (!string.IsNullOrEmpty(dto.OllamaModel)) settings.OllamaModel = dto.OllamaModel;
        if (dto.AutoMatchOnSync.HasValue) settings.AutoMatchOnSync = dto.AutoMatchOnSync.Value;
        if (dto.OnlyFranceCompatibleJobs.HasValue) settings.OnlyFranceCompatibleJobs = dto.OnlyFranceCompatibleJobs.Value;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            settings.Id,
            settings.AiProvider,
            HasGeminiKey = !string.IsNullOrEmpty(settings.GeminiApiKey),
            GeminiApiKey = MaskKey(settings.GeminiApiKey),
            settings.GeminiModel,
            HasOpenAiKey = !string.IsNullOrEmpty(settings.OpenAiApiKey),
            OpenAiApiKey = MaskKey(settings.OpenAiApiKey),
            settings.OpenAiModel,
            settings.OllamaEndpoint,
            settings.OllamaModel,
            settings.AutoMatchOnSync,
            settings.OnlyFranceCompatibleJobs,
            settings.UpdatedAt
        });
    }

    [HttpPost("test-ai")]
    public async Task<IActionResult> TestAi([FromBody] TestAiDto dto)
    {
        try
        {
            var currentSettings = await _db.Settings.FirstOrDefaultAsync();

            if (dto.Provider == "Gemini")
            {
                var apiKey = !string.IsNullOrEmpty(dto.ApiKey) && !IsMaskedKey(dto.ApiKey)
                    ? dto.ApiKey
                    : currentSettings?.GeminiApiKey;
                if (string.IsNullOrEmpty(apiKey)) return BadRequest("Clé Gemini requise.");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{dto.Model ?? currentSettings?.GeminiModel ?? "gemini-2.5-flash"}:generateContent?key={apiKey}";
                var body = new { contents = new[] { new { parts = new[] { new { text = "Ping. Reponds 'OK' en un mot." } } } } };
                using var res = await _httpClient.PostAsJsonAsync(url, body);
                if (res.IsSuccessStatusCode)
                {
                    return Ok(new { Success = true, Message = "Connexion à Google Gemini réussie !" });
                }
                return BadRequest($"Erreur Gemini : code {res.StatusCode}");
            }

            if (dto.Provider == "Ollama")
            {
                var endpoint = $"{(dto.Endpoint ?? currentSettings?.OllamaEndpoint ?? "http://localhost:11434").TrimEnd('/')}/api/tags";
                using var res = await _httpClient.GetAsync(endpoint);
                if (res.IsSuccessStatusCode)
                {
                    return Ok(new { Success = true, Message = "Ollama est bien accessible en local !" });
                }
                return BadRequest($"Ollama non joignable sur {dto.Endpoint} : {res.StatusCode}");
            }

            return Ok(new { Success = true, Message = "Mode heuristique local : 100% prêt sans configuration externe !" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Erreur lors du test : {ex.Message}");
        }
    }

    private static bool IsMaskedKey(string key)
        => key.Contains("****", StringComparison.Ordinal) || key.Contains("•••", StringComparison.Ordinal);

    private static string? MaskKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (key.Length < 8) return "********";
        return key[..4] + "********" + key[^4..];
    }
}

public record UpdateSettingsDto(
    string? AiProvider = null,
    string? GeminiApiKey = null,
    string? GeminiModel = null,
    string? OpenAiApiKey = null,
    string? OpenAiModel = null,
    string? OllamaEndpoint = null,
    string? OllamaModel = null,
    bool? AutoMatchOnSync = null,
    bool? OnlyFranceCompatibleJobs = null
);

public record TestAiDto(string Provider, string? ApiKey = null, string? Model = null, string? Endpoint = null);
