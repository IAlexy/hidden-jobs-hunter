export interface JobOffer {
  id: number;
  externalId: string;
  source: string;
  title: string;
  company: string;
  companyUrl?: string;
  location: string;
  remoteScope: string;
  isFranceCompatible: boolean;
  contractType: string;
  salary?: string;
  directContact?: string;
  applyUrl?: string;
  description?: string;
  descriptionPreview?: string;
  tags: string;
  publishedAt: string;
  createdAt: string;
  status: 'Discovered' | 'Saved' | 'Applied' | 'Interview' | 'Offer' | 'Archived' | 'Rejected';
  userNotes?: string;
  activeMatch?: JobMatch;
}

export interface JobMatch {
  id: number;
  jobOfferId: number;
  userProfileId: number;
  matchScore: number;
  techStackFit: number;
  remoteEligibilityFit: number;
  summary: string;
  strengthsJson: string;
  missingSkillsJson: string;
  administrativeFeasibility: string;
  tailoredPitch: string;
  aiProviderUsed: string;
  analyzedAt: string;
}

export interface UserProfile {
  id: number;
  name: string;
  isActive: boolean;
  title: string;
  yearsOfExperience: number;
  preferredContracts: string;
  remoteScopePreference: string;
  canWorkAsContractorB2B: boolean;
  primarySkills: string;
  secondarySkills: string;
  languages: string;
  expectedSalaryOrTjm?: string;
  preferredPitchLanguage: string;
  resumeText?: string;
  updatedAt?: string;
  matchCount?: number;
  highMatchCount?: number;
}

export interface AppSettings {
  id: number;
  aiProvider: 'Heuristic' | 'Gemini' | 'OpenAI' | 'Ollama';
  hasGeminiKey?: boolean;
  geminiApiKey?: string;
  geminiModel: string;
  hasOpenAiKey?: boolean;
  openAiApiKey?: string;
  openAiModel: string;
  ollamaEndpoint: string;
  ollamaModel: string;
  autoMatchOnSync: boolean;
  onlyFranceCompatibleJobs: boolean;
}

export interface StatsData {
  totalJobs: number;
  franceCompatible: number;
  hnDirectPosts: number;
  withDirectContact: number;
  highMatches: number;
  saved: number;
  applied: number;
  interview: number;
}

export interface SyncResult {
  totalFetched: number;
  newJobsSaved: number;
  matchesCreated: number;
  details: string[];
  syncedAt: string;
}
