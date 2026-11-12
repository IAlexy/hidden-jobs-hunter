import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Header } from '../../src/components/Header';
import { JobCard } from '../../src/components/JobCard';
import { KanbanView } from '../../src/components/KanbanView';
import { IngestUrlModal } from '../../src/components/IngestUrlModal';
import { PitchModal } from '../../src/components/PitchModal';
import { SettingsModal } from '../../src/components/SettingsModal';
import { JobOffer, StatsData, UserProfile } from '../../src/types';
import * as api from '../../src/api';

vi.mock('../../src/api', () => ({
  ingestAtsUrl: vi.fn(),
  generatePitch: vi.fn(),
  fetchSettings: vi.fn(),
  updateSettings: vi.fn(),
  testAiConnection: vi.fn(),
}));

const sampleProfile: UserProfile = {
  id: 1,
  name: 'Demo Profile (Fullstack .NET / React)',
  isActive: true,
  title: 'Senior Fullstack Engineer',
  yearsOfExperience: 5,
  preferredContracts: 'CDI,Freelance/Contractor',
  remoteScopePreference: 'Worldwide,Europe,France',
  canWorkAsContractorB2B: true,
  primarySkills: '.NET, C#, React, TypeScript',
  secondarySkills: 'Docker, Azure',
  languages: 'Français, Anglais',
  preferredPitchLanguage: 'Auto',
};

const sampleStats: StatsData = {
  totalJobs: 24,
  franceCompatible: 20,
  hnDirectPosts: 8,
  withDirectContact: 6,
  highMatches: 11,
  saved: 3,
  applied: 2,
  interview: 1,
};

const sampleJob: JobOffer = {
  id: 101,
  externalId: 'hn_101',
  source: 'HackerNews',
  title: 'Senior Fullstack .NET / React Engineer',
  company: 'AcmeCloud',
  location: 'Remote (Europe)',
  remoteScope: 'Europe',
  isFranceCompatible: true,
  contractType: 'CDI / Full-time',
  salary: '85k€ - 105k€',
  directContact: 'cto@acmecloud.io',
  applyUrl: 'https://acmecloud.io/careers/101',
  description: 'Building distributed cloud services with .NET 8 and React 19.',
  descriptionPreview: 'Building distributed cloud services...',
  tags: '.NET, C#, React, TypeScript, Docker, Azure',
  publishedAt: '2026-09-18T10:00:00Z',
  createdAt: '2026-09-18T10:00:00Z',
  status: 'Saved',
  userNotes: 'Contact CTO directly',
  activeMatch: {
    id: 1,
    jobOfferId: 101,
    userProfileId: 1,
    matchScore: 91,
    techStackFit: 95,
    remoteEligibilityFit: 95,
    summary: 'Excellent match for .NET and React',
    strengthsJson: JSON.stringify(['Alignement fort sur .NET & React', 'Contact direct CTO']),
    missingSkillsJson: '[]',
    administrativeFeasibility: 'Éligible CDI ou Freelance B2B depuis la France',
    tailoredPitch: 'Hi AcmeCloud team, I would love to join as Senior Fullstack Engineer.',
    aiProviderUsed: 'Heuristic',
    analyzedAt: '2026-09-18T10:05:00Z',
  },
};

describe('UI Components Suite', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Header renders stats, active profile and triggers navigation & actions', () => {
    const setActiveTab = vi.fn();
    const onSelectProfile = vi.fn();
    const onOpenProfiles = vi.fn();
    const onOpenSettings = vi.fn();
    const onOpenIngest = vi.fn();
    const onSync = vi.fn();

    render(
      <Header
        activeTab="explorer"
        setActiveTab={setActiveTab}
        activeProfile={sampleProfile}
        profiles={[sampleProfile]}
        onSelectProfile={onSelectProfile}
        onOpenProfiles={onOpenProfiles}
        onOpenSettings={onOpenSettings}
        onOpenIngest={onOpenIngest}
        onSync={onSync}
        isSyncing={false}
        stats={sampleStats}
      />
    );

    expect(screen.getByText('HiddenJobs Hunter')).toBeInTheDocument();
    expect(screen.getByText('24')).toBeInTheDocument();
    expect(screen.getByText('11')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Pipeline & Suivi'));
    expect(setActiveTab).toHaveBeenCalledWith('pipeline');

    fireEvent.click(screen.getByText('Scanner le radar'));
    expect(onSync).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByText('Scraper un ATS'));
    expect(onOpenIngest).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByText('Gérer'));
    expect(onOpenProfiles).toHaveBeenCalledTimes(1);
  });

  it('JobCard displays match score, strengths, tags and fires callbacks', () => {
    const onViewDetails = vi.fn();
    const onOpenPitch = vi.fn();
    const onUpdateStatus = vi.fn();

    render(
      <JobCard
        job={sampleJob}
        onViewDetails={onViewDetails}
        onOpenPitch={onOpenPitch}
        onUpdateStatus={onUpdateStatus}
      />
    );

    expect(screen.getByText('Senior Fullstack .NET / React Engineer')).toBeInTheDocument();
    expect(screen.getByText('91% Match')).toBeInTheDocument();
    expect(screen.getByText('cto@acmecloud.io')).toBeInTheDocument();
    expect(screen.getByText('Alignement fort sur .NET & React')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Pitch IA'));
    expect(onOpenPitch).toHaveBeenCalledWith(sampleJob);

    fireEvent.click(screen.getByText('Détails'));
    expect(onViewDetails).toHaveBeenCalledWith(sampleJob);

    fireEvent.change(screen.getByLabelText('Statut de la candidature'), {
      target: { value: 'Applied' },
    });
    expect(onUpdateStatus).toHaveBeenCalledWith(101, 'Applied');
  });

  it('KanbanView renders columns and moves job cards between stages', () => {
    const onViewDetails = vi.fn();
    const onOpenPitch = vi.fn();
    const onUpdateStatus = vi.fn();

    render(
      <KanbanView
        jobs={[sampleJob]}
        onViewDetails={onViewDetails}
        onOpenPitch={onOpenPitch}
        onUpdateStatus={onUpdateStatus}
      />
    );

    expect(screen.getByText('Sauvegardées & À contacter')).toBeInTheDocument();
    expect(screen.getByText('Senior Fullstack .NET / React Engineer')).toBeInTheDocument();

    const moveRightBtn = screen.getByTitle("Déplacer vers l'étape suivante");
    fireEvent.click(moveRightBtn);
    expect(onUpdateStatus).toHaveBeenCalledWith(101, 'Applied');
  });

  it('IngestUrlModal fills preset URL and calls ingestAtsUrl', async () => {
    vi.mocked(api.ingestAtsUrl).mockResolvedValueOnce({
      url: 'https://jobs.ashbyhq.com/linear',
      extractedCount: 6,
      newJobsAdded: 4,
    });

    const onClose = vi.fn();
    const onSuccess = vi.fn();

    render(<IngestUrlModal isOpen={true} onClose={onClose} onSuccess={onSuccess} />);

    fireEvent.click(screen.getByText('Exemple Ashby'));
    const input = screen.getByPlaceholderText(/https:\/\/jobs\.ashbyhq\.com/i) as HTMLInputElement;
    expect(input.value).toBe('https://jobs.ashbyhq.com/linear');

    fireEvent.click(screen.getByText("Lancer l'extraction"));

    await waitFor(() => {
      expect(api.ingestAtsUrl).toHaveBeenCalledWith('https://jobs.ashbyhq.com/linear');
      expect(screen.getByText(/Scan terminé avec succès/i)).toBeInTheDocument();
      expect(onSuccess).toHaveBeenCalledTimes(1);
    });
  });

  it('PitchModal displays initial pitch and regenerates on demand', async () => {
    vi.mocked(api.generatePitch).mockResolvedValueOnce('New custom pitch from AI');

    const onClose = vi.fn();
    render(<PitchModal job={sampleJob} onClose={onClose} activeProfileId={1} />);

    const textarea = screen.getByDisplayValue(/Hi AcmeCloud team/i);
    expect(textarea).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText(/Rédiger en français/i), {
      target: { value: 'Insister sur .NET 8' },
    });
    fireEvent.click(screen.getByText('Régénérer'));

    await waitFor(() => {
      expect(api.generatePitch).toHaveBeenCalledWith(101, 1, 'Insister sur .NET 8');
      expect(screen.getByDisplayValue('New custom pitch from AI')).toBeInTheDocument();
    });
  });

  it('SettingsModal loads settings, switches AI provider and saves', async () => {
    vi.mocked(api.fetchSettings).mockResolvedValueOnce({
      id: 1,
      aiProvider: 'Heuristic',
      geminiModel: 'gemini-2.5-flash',
      openAiModel: 'gpt-4o-mini',
      ollamaEndpoint: 'http://localhost:11434',
      ollamaModel: 'llama3',
      autoMatchOnSync: true,
      onlyFranceCompatibleJobs: true,
    });

    vi.mocked(api.updateSettings).mockResolvedValueOnce({
      id: 1,
      aiProvider: 'Gemini',
      geminiModel: 'gemini-2.5-flash',
      openAiModel: 'gpt-4o-mini',
      ollamaEndpoint: 'http://localhost:11434',
      ollamaModel: 'llama3',
      autoMatchOnSync: true,
      onlyFranceCompatibleJobs: true,
    });

    const onClose = vi.fn();
    render(<SettingsModal isOpen={true} onClose={onClose} />);

    await waitFor(() => {
      expect(screen.getByText('Algorithmique Local')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Google Gemini API'));
    expect(screen.getByText('Configuration Google Gemini')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Enregistrer'));
    await waitFor(() => {
      expect(api.updateSettings).toHaveBeenCalled();
      expect(screen.getByText(/Paramètres enregistrés avec succès/i)).toBeInTheDocument();
    });
  });
});
