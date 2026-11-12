import { AppSettings, JobOffer, ProfileTemplate, StatsData, SyncResult, UserProfile } from './types';

const API_BASE = '/api';

export async function fetchJobs(params: {
  search?: string;
  source?: string;
  remoteScope?: string;
  contractType?: string;
  status?: string;
  minScore?: number;
  franceCompatibleOnly?: boolean;
  profileId?: number;
  page?: number;
  pageSize?: number;
}): Promise<{ total: number; page: number; pageSize: number; items: JobOffer[] }> {
  const query = new URLSearchParams();
  if (params.search) query.append('search', params.search);
  if (params.source && params.source !== 'All') query.append('source', params.source);
  if (params.remoteScope && params.remoteScope !== 'All') query.append('remoteScope', params.remoteScope);
  if (params.contractType && params.contractType !== 'All') query.append('contractType', params.contractType);
  if (params.status && params.status !== 'All') query.append('status', params.status);
  if (params.minScore) query.append('minScore', params.minScore.toString());
  if (params.franceCompatibleOnly) query.append('franceCompatibleOnly', 'true');
  if (params.profileId) query.append('profileId', params.profileId.toString());
  if (params.page) query.append('page', params.page.toString());
  if (params.pageSize) query.append('pageSize', params.pageSize.toString());

  const res = await fetch(`${API_BASE}/jobs?${query.toString()}`);
  if (!res.ok) throw new Error('Erreur lors du chargement des offres');
  return res.json();
}

export async function fetchJobDetails(id: number, profileId?: number): Promise<JobOffer> {
  const query = profileId ? `?profileId=${profileId}` : '';
  const res = await fetch(`${API_BASE}/jobs/${id}${query}`);
  if (!res.ok) throw new Error('Erreur lors du chargement de l\'offre');
  return res.json();
}

export async function updateJobStatus(id: number, status: string, notes?: string): Promise<void> {
  const res = await fetch(`${API_BASE}/jobs/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status, notes }),
  });
  if (!res.ok) throw new Error('Erreur lors de la mise à jour du statut');
}

export async function updateJobNotes(id: number, notes: string): Promise<void> {
  const res = await fetch(`${API_BASE}/jobs/${id}/notes`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ notes }),
  });
  if (!res.ok) throw new Error('Erreur lors de la sauvegarde des notes');
}

export async function deleteJob(id: number): Promise<void> {
  const res = await fetch(`${API_BASE}/jobs/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Erreur lors de la suppression de l\'offre');
}

export async function fetchStats(profileId?: number): Promise<StatsData> {
  const query = profileId ? `?profileId=${profileId}` : '';
  const res = await fetch(`${API_BASE}/jobs/stats${query}`);
  if (!res.ok) throw new Error('Erreur lors du chargement des statistiques');
  return res.json();
}

export async function triggerSync(): Promise<SyncResult> {
  const res = await fetch(`${API_BASE}/sync`, { method: 'POST' });
  if (!res.ok) throw new Error('Erreur lors de la synchronisation');
  return res.json();
}

export async function ingestAtsUrl(url: string): Promise<{ url: string; extractedCount: number; newJobsAdded: number }> {
  const res = await fetch(`${API_BASE}/sync/ingest-url`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url }),
  });
  if (!res.ok) throw new Error('Erreur lors de l\'ingestion de l\'URL');
  return res.json();
}

export async function fetchProfiles(): Promise<UserProfile[]> {
  const res = await fetch(`${API_BASE}/profiles`);
  if (!res.ok) throw new Error('Erreur lors du chargement des profils');
  return res.json();
}

export async function fetchProfileTemplates(): Promise<ProfileTemplate[]> {
  const res = await fetch(`${API_BASE}/profiles/templates`);
  if (!res.ok) throw new Error('Erreur lors du chargement des modèles de profils');
  return res.json();
}

export async function createProfile(profile: Partial<UserProfile>): Promise<UserProfile> {
  const res = await fetch(`${API_BASE}/profiles`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(profile),
  });
  if (!res.ok) throw new Error('Erreur lors de la création du profil');
  return res.json();
}

export async function updateProfile(id: number, profile: Partial<UserProfile>): Promise<UserProfile> {
  const res = await fetch(`${API_BASE}/profiles/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(profile),
  });
  if (!res.ok) throw new Error('Erreur lors de la mise à jour du profil');
  return res.json();
}

export async function activateProfile(id: number): Promise<void> {
  const res = await fetch(`${API_BASE}/profiles/${id}/activate`, { method: 'POST' });
  if (!res.ok) throw new Error('Erreur lors de l\'activation du profil');
}

export async function deleteProfile(id: number): Promise<void> {
  const res = await fetch(`${API_BASE}/profiles/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Erreur lors de la suppression du profil');
}

export async function parseCvWithAi(cvText: string, profileName?: string): Promise<Partial<UserProfile>> {
  const res = await fetch(`${API_BASE}/profiles/parse-cv`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ cvText, profileName }),
  });
  if (!res.ok) throw new Error('Erreur lors de l\'analyse du CV');
  return res.json();
}

export async function reMatchJob(jobId: number, profileId?: number): Promise<void> {
  const query = profileId ? `?profileId=${profileId}` : '';
  const res = await fetch(`${API_BASE}/matching/job/${jobId}${query}`, { method: 'POST' });
  if (!res.ok) throw new Error('Erreur lors du re-calcul du match');
}

export async function generatePitch(jobId: number, profileId?: number, customInstructions?: string): Promise<string> {
  const res = await fetch(`${API_BASE}/matching/pitch`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ jobId, profileId, customInstructions }),
  });
  if (!res.ok) throw new Error('Erreur lors de la génération de l\'accroche');
  const data = await res.json();
  return data.pitch;
}

export async function fetchSettings(): Promise<AppSettings> {
  const res = await fetch(`${API_BASE}/settings`);
  if (!res.ok) throw new Error('Erreur lors du chargement des paramètres');
  return res.json();
}

export async function updateSettings(settings: Partial<AppSettings>): Promise<AppSettings> {
  const res = await fetch(`${API_BASE}/settings`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(settings),
  });
  if (!res.ok) throw new Error('Erreur lors de la sauvegarde des paramètres');
  return res.json();
}

export async function testAiConnection(dto: { provider: string; apiKey?: string; model?: string; endpoint?: string }): Promise<{ success: boolean; message: string }> {
  const res = await fetch(`${API_BASE}/settings/test-ai`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || 'Échec du test de connexion');
  return data;
}
