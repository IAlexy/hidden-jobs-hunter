using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HiddenJobsHunter.Api.Services;

namespace HiddenJobsHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly JobSyncService _syncService;

    public SyncController(JobSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderInfoDto>), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        return Ok(_syncService.GetProviders());
    }

    [HttpPost]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunSync()
    {
        var result = await _syncService.SyncAllProvidersAsync();
        return Ok(result);
    }

    [HttpPost("ingest-url")]
    [ProducesResponseType(typeof(IngestUrlResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestUrl([FromBody] IngestUrlRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url) ||
            !Uri.TryCreate(req.Url.Trim(), UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("Une URL valide (http/https) est requise.");
        }

        var result = await _syncService.IngestUrlAsync(parsedUri.ToString());
        return Ok(result);
    }
}

public record IngestUrlRequest(string? Url = null);
