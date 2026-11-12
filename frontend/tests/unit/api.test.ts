import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  fetchJobs,
  fetchJobDetails,
  updateJobStatus,
  updateJobNotes,
  deleteJob,
  fetchStats,
  triggerSync,
  ingestAtsUrl,
  fetchProfiles,
  fetchProfileTemplates,
  createProfile,
  updateProfile,
  activateProfile,
  deleteProfile,
  parseCvWithAi,
  reMatchJob,
  generatePitch,
  fetchSettings,
  updateSettings,
  testAiConnection,
} from '../../src/api';

describe('API Client (src/api.ts)', () => {
  const mockFetch = vi.fn();

  beforeEach(() => {
    mockFetch.mockReset();
    vi.stubGlobal('fetch', mockFetch);
  });

  function mockJsonResponse(data: unknown, ok = true, status = 200) {
    return Promise.resolve({
      ok,
      status,
      json: () => Promise.resolve(data),
    });
  }

  it('fetchJobs builds query parameters and returns paginated items', async () => {
    const payload = { total: 1, page: 1, pageSize: 18, items: [{ id: 1, title: 'Senior .NET' }] };
    mockFetch.mockReturnValueOnce(mockJsonResponse(payload));

    const res = await fetchJobs({
      search: 'react',
      source: 'HackerNews',
      remoteScope: 'Worldwide',
      contractType: 'CDI',
      status: 'Saved',
      minScore: 75,
      franceCompatibleOnly: true,
      profileId: 2,
      page: 1,
      pageSize: 18,
    });

    expect(res).toEqual(payload);
    expect(mockFetch).toHaveBeenCalledTimes(1);
    const calledUrl = mockFetch.mock.calls[0][0] as string;
    expect(calledUrl).toContain('/api/jobs?');
    expect(calledUrl).toContain('search=react');
    expect(calledUrl).toContain('source=HackerNews');
    expect(calledUrl).toContain('remoteScope=Worldwide');
    expect(calledUrl).toContain('contractType=CDI');
    expect(calledUrl).toContain('status=Saved');
    expect(calledUrl).toContain('minScore=75');
    expect(calledUrl).toContain('franceCompatibleOnly=true');
    expect(calledUrl).toContain('profileId=2');
  });

  it('fetchJobs throws on non-ok response', async () => {
    mockFetch.mockReturnValueOnce(mockJsonResponse({}, false, 500));
    await expect(fetchJobs({})).rejects.toThrow('Erreur lors du chargement des offres');
  });

  it('fetchJobDetails, updateJobStatus, updateJobNotes, deleteJob and fetchStats work properly', async () => {
    mockFetch.mockReturnValueOnce(mockJsonResponse({ id: 42, title: 'Staff Engineer' }));
    const job = await fetchJobDetails(42, 3);
    expect(job.id).toBe(42);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/jobs/42?profileId=3');

    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await updateJobStatus(42, 'Applied', 'Sent email');
    expect(mockFetch).toHaveBeenLastCalledWith('/api/jobs/42/status', expect.objectContaining({ method: 'PATCH' }));

    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await updateJobNotes(42, 'Updated notes');
    expect(mockFetch).toHaveBeenLastCalledWith('/api/jobs/42/notes', expect.objectContaining({ method: 'PATCH' }));

    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await deleteJob(42);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/jobs/42', { method: 'DELETE' });

    const statsPayload = {
      totalJobs: 10,
      franceCompatible: 9,
      hnDirectPosts: 4,
      withDirectContact: 3,
      highMatches: 5,
      saved: 2,
      applied: 1,
      interview: 1,
    };
    mockFetch.mockReturnValueOnce(mockJsonResponse(statsPayload));
    const stats = await fetchStats(1);
    expect(stats.totalJobs).toBe(10);
  });

  it('triggerSync and ingestAtsUrl call sync endpoints', async () => {
    const syncPayload = { totalFetched: 12, newJobsSaved: 4, matchesCreated: 4, details: [], syncedAt: '2026-09-18' };
    mockFetch.mockReturnValueOnce(mockJsonResponse(syncPayload));
    const syncRes = await triggerSync();
    expect(syncRes.newJobsSaved).toBe(4);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/sync', { method: 'POST' });

    const ingestPayload = { url: 'https://jobs.ashbyhq.com/linear', extractedCount: 5, newJobsAdded: 3 };
    mockFetch.mockReturnValueOnce(mockJsonResponse(ingestPayload));
    const ingestRes = await ingestAtsUrl('https://jobs.ashbyhq.com/linear');
    expect(ingestRes.newJobsAdded).toBe(3);
  });

  it('profiles endpoints (fetch, create, update, activate, delete, parseCvWithAi) work properly', async () => {
    mockFetch.mockReturnValueOnce(mockJsonResponse([{ id: 1, name: 'Demo Profile' }]));
    const list = await fetchProfiles();
    expect(list).toHaveLength(1);

    mockFetch.mockReturnValueOnce(mockJsonResponse([{ id: 'data', name: 'Data Scientist / AI Engineer' }]));
    const templates = await fetchProfileTemplates();
    expect(templates).toHaveLength(1);
    expect(templates[0].id).toBe('data');

    mockFetch.mockReturnValueOnce(mockJsonResponse({ id: 2, name: 'New Profile' }));
    const created = await createProfile({ name: 'New Profile' });
    expect(created.id).toBe(2);

    mockFetch.mockReturnValueOnce(mockJsonResponse({ id: 2, name: 'Updated Profile' }));
    const updated = await updateProfile(2, { name: 'Updated Profile' });
    expect(updated.name).toBe('Updated Profile');

    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await activateProfile(2);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/profiles/2/activate', { method: 'POST' });

    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await deleteProfile(2);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/profiles/2', { method: 'DELETE' });

    mockFetch.mockReturnValueOnce(mockJsonResponse({ title: 'Senior .NET Engineer', yearsOfExperience: 6 }));
    const parsed = await parseCvWithAi('6 years C# .NET', 'Demo');
    expect(parsed.yearsOfExperience).toBe(6);
  });

  it('matching endpoints (reMatchJob, generatePitch) and settings endpoints work properly', async () => {
    mockFetch.mockReturnValueOnce(mockJsonResponse({}));
    await reMatchJob(10, 1);
    expect(mockFetch).toHaveBeenLastCalledWith('/api/matching/job/10?profileId=1', { method: 'POST' });

    mockFetch.mockReturnValueOnce(mockJsonResponse({ pitch: 'Tailored outreach message' }));
    const pitch = await generatePitch(10, 1, 'Short pitch');
    expect(pitch).toBe('Tailored outreach message');

    mockFetch.mockReturnValueOnce(mockJsonResponse({ id: 1, aiProvider: 'Heuristic' }));
    const settings = await fetchSettings();
    expect(settings.aiProvider).toBe('Heuristic');

    mockFetch.mockReturnValueOnce(mockJsonResponse({ id: 1, aiProvider: 'Gemini' }));
    const updatedSettings = await updateSettings({ aiProvider: 'Gemini' });
    expect(updatedSettings.aiProvider).toBe('Gemini');

    mockFetch.mockReturnValueOnce(mockJsonResponse({ success: true, message: 'OK' }));
    const testOk = await testAiConnection({ provider: 'Heuristic' });
    expect(testOk.success).toBe(true);

    mockFetch.mockReturnValueOnce(mockJsonResponse({ message: 'Invalid API key' }, false, 400));
    await expect(testAiConnection({ provider: 'Gemini', apiKey: 'bad' })).rejects.toThrow('Invalid API key');
  });
});
