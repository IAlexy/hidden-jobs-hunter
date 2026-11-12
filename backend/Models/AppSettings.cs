using System;

namespace HiddenJobsHunter.Api.Models;

public class AppSettings
{
    public int Id { get; set; } = 1;
    
    // Heuristic (Default/No key required), Gemini, OpenAI, Ollama
    public string AiProvider { get; set; } = "Heuristic";
    
    public string? GeminiApiKey { get; set; }
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    
    public string? OpenAiApiKey { get; set; }
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3";
    
    // Auto-match when new jobs are synced
    public bool AutoMatchOnSync { get; set; } = true;
    
    // Filter for sync: only import jobs compatible with remote in France/worldwide
    public bool OnlyFranceCompatibleJobs { get; set; } = true;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
