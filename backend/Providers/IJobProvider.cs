using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HiddenJobsHunter.Api.Models;

namespace HiddenJobsHunter.Api.Providers;

public interface IJobProvider
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    bool IsEnabledByDefault { get; }
    
    Task<List<JobOffer>> FetchJobsAsync(int limit = 50, CancellationToken ct = default);
}
