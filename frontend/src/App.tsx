import React, { useState, useEffect, useCallback } from 'react';
import { 
  Search, 
  Sparkles, 
  Globe, 
  RotateCw, 
  ChevronLeft, 
  ChevronRight,
  CheckCircle2
} from 'lucide-react';
import { Header } from './components/Header';
import { JobCard } from './components/JobCard';
import { JobDetailModal } from './components/JobDetailModal';
import { PitchModal } from './components/PitchModal';
import { ProfileManagerModal } from './components/ProfileManagerModal';
import { SettingsModal } from './components/SettingsModal';
import { IngestUrlModal } from './components/IngestUrlModal';
import { KanbanView } from './components/KanbanView';
import { JobOffer, StatsData, UserProfile } from './types';
import { useI18n } from './i18n';
import { 
  fetchJobs, 
  fetchJobDetails, 
  fetchProfiles, 
  fetchStats, 
  triggerSync, 
  updateJobStatus, 
  updateJobNotes, 
  reMatchJob,
  activateProfile
} from './api';

export function App() {
  const { t } = useI18n();

  // Navigation & Tabs
  const [activeTab, setActiveTab] = useState<'explorer' | 'pipeline'>('explorer');

  // Profiles & Stats
  const [profiles, setProfiles] = useState<UserProfile[]>([]);
  const [activeProfile, setActiveProfile] = useState<UserProfile | undefined>();
  const [stats, setStats] = useState<StatsData | undefined>();

  // Jobs state
  const [jobs, setJobs] = useState<JobOffer[]>([]);
  const [totalJobs, setTotalJobs] = useState(0);
  const [page, setPage] = useState(1);
  const pageSize = 18;
  const [isLoadingJobs, setIsLoadingJobs] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);

  // Filters
  const [search, setSearch] = useState('');
  const [sourceFilter, setSourceFilter] = useState('All');
  const [scopeFilter, setScopeFilter] = useState('All');
  const [contractFilter, setContractFilter] = useState('All');
  const [statusFilter] = useState('All');
  const [minScoreFilter, setMinScoreFilter] = useState<number | undefined>(undefined);
  const [franceOnly, setFranceOnly] = useState(true);

  // Modals state
  const [selectedJobForDetail, setSelectedJobForDetail] = useState<JobOffer | null>(null);
  const [selectedJobForPitch, setSelectedJobForPitch] = useState<JobOffer | null>(null);
  const [isProfilesOpen, setIsProfilesOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isIngestOpen, setIsIngestOpen] = useState(false);

  // Toast Notification
  const [notification, setNotification] = useState<string | null>(null);
  const showToast = (msg: string) => {
    setNotification(msg);
    setTimeout(() => setNotification(null), 4000);
  };

  // Load profiles & stats
  const loadProfilesAndStats = useCallback(async () => {
    try {
      const pList = await fetchProfiles();
      setProfiles(pList);
      const active = pList.find((p) => p.isActive) || pList[0];
      setActiveProfile(active);

      const sData = await fetchStats(active?.id);
      setStats(sData);
    } catch (err) {
      console.error('Failed to load profiles/stats', err);
    }
  }, []);

  const activeProfileId = activeProfile?.id;

  // Load jobs based on filters
  const loadJobs = useCallback(async () => {
    setIsLoadingJobs(true);
    try {
      const data = await fetchJobs({
        search,
        source: sourceFilter,
        remoteScope: scopeFilter,
        contractType: contractFilter,
        status: statusFilter,
        minScore: minScoreFilter,
        franceCompatibleOnly: franceOnly,
        profileId: activeProfileId,
        page,
        pageSize,
      });
      setJobs(data.items);
      setTotalJobs(data.total);
    } catch (err) {
      console.error('Failed to load jobs', err);
    } finally {
      setIsLoadingJobs(false);
    }
  }, [search, sourceFilter, scopeFilter, contractFilter, statusFilter, minScoreFilter, franceOnly, activeProfileId, page]);

  useEffect(() => {
    let active = true;
    fetchProfiles()
      .then(async (pList) => {
        if (!active) return;
        setProfiles(pList);
        const currentActive = pList.find((p) => p.isActive) || pList[0];
        setActiveProfile(currentActive);
        const sData = await fetchStats(currentActive?.id);
        if (active) setStats(sData);
      })
      .catch((err) => console.error('Failed to load profiles/stats', err));
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;
    fetchJobs({
      search,
      source: sourceFilter,
      remoteScope: scopeFilter,
      contractType: contractFilter,
      status: statusFilter,
      minScore: minScoreFilter,
      franceCompatibleOnly: franceOnly,
      profileId: activeProfileId,
      page,
      pageSize,
    })
      .then((data) => {
        if (!active) return;
        setJobs(data.items);
        setTotalJobs(data.total);
      })
      .catch((err) => console.error('Failed to load jobs', err));
    return () => {
      active = false;
    };
  }, [search, sourceFilter, scopeFilter, contractFilter, statusFilter, minScoreFilter, franceOnly, activeProfileId, page]);

  // Profile selection
  const handleSelectProfile = async (id: number) => {
    try {
      await activateProfile(id);
      await loadProfilesAndStats();
      setPage(1);
    } catch {
      alert('Erreur lors du changement de profil.');
    }
  };

  // Trigger Radar Sync
  const handleSync = async () => {
    setIsSyncing(true);
    try {
      const res = await triggerSync();
      showToast(t('app.toastSyncDone', { count: res.newJobsSaved }));
      await loadProfilesAndStats();
      await loadJobs();
    } catch {
      showToast(t('app.toastSyncError'));
    } finally {
      setIsSyncing(false);
    }
  };

  // Status update
  const handleUpdateStatus = async (id: number, newStatus: JobOffer['status']) => {
    try {
      await updateJobStatus(id, newStatus);
      // update locally
      setJobs((prev) =>
        prev.map((j) => (j.id === id ? { ...j, status: newStatus } : j))
      );
      if (selectedJobForDetail && selectedJobForDetail.id === id) {
        setSelectedJobForDetail((prev) => (prev ? { ...prev, status: newStatus } : null));
      }
      const sData = await fetchStats(activeProfile?.id);
      setStats(sData);
    } catch {
      alert('Erreur lors de la mise à jour du statut.');
    }
  };

  // Notes update
  const handleSaveNotes = async (id: number, notes: string) => {
    await updateJobNotes(id, notes);
    setJobs((prev) =>
      prev.map((j) => (j.id === id ? { ...j, userNotes: notes } : j))
    );
    showToast(t('app.toastNotesSaved'));
  };

  // Re-match job
  const handleReMatch = async (id: number) => {
    await reMatchJob(id, activeProfile?.id);
    const updated = await fetchJobDetails(id, activeProfile?.id);
    setJobs((prev) => prev.map((j) => (j.id === id ? updated : j)));
    setSelectedJobForDetail(updated);
    showToast(t('app.toastRematched'));
  };

  const totalPages = Math.ceil(totalJobs / pageSize) || 1;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col selection:bg-emerald-500 selection:text-white">
      
      {/* Top Header */}
      <Header
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        activeProfile={activeProfile}
        profiles={profiles}
        onSelectProfile={handleSelectProfile}
        onOpenProfiles={() => setIsProfilesOpen(true)}
        onOpenSettings={() => setIsSettingsOpen(true)}
        onOpenIngest={() => setIsIngestOpen(true)}
        onSync={handleSync}
        isSyncing={isSyncing}
        stats={stats}
      />

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        
        {/* Toast Alert */}
        {notification && (
          <div className="fixed bottom-6 right-6 z-50 bg-emerald-600 text-white font-medium text-xs px-4 py-3 rounded-xl shadow-2xl flex items-center gap-2 animate-in fade-in slide-in-from-bottom-5">
            <CheckCircle2 className="h-4 w-4" />
            <span>{notification}</span>
          </div>
        )}

        {/* View: Pipeline Tracker */}
        {activeTab === 'pipeline' ? (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-lg font-bold text-white tracking-tight">{t('app.pipelineTitle')}</h2>
                <p className="text-xs text-slate-400">
                  {t('app.pipelineSubtitle')}
                </p>
              </div>
            </div>

            <KanbanView
              jobs={jobs}
              onViewDetails={(j) => setSelectedJobForDetail(j)}
              onOpenPitch={(j) => setSelectedJobForPitch(j)}
              onUpdateStatus={handleUpdateStatus}
            />
          </div>
        ) : (
          /* View: Jobs Explorer & Radar */
          <div className="space-y-6">
            
            {/* Filter Toolbar */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4 shadow-sm space-y-3.5 backdrop-blur-sm">
              
              {/* Row 1: Search input + Scope + Score + Clear */}
              <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3">
                
                {/* Search Bar */}
                <div className="relative flex-1">
                  <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => {
                      setSearch(e.target.value);
                      setPage(1);
                    }}
                    placeholder={t('app.searchPlaceholder')}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-4 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-emerald-500 transition"
                  />
                </div>

                {/* Remote Scope Filter */}
                <div className="flex items-center gap-1.5 bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 shrink-0">
                  <Globe className="h-3.5 w-3.5 text-slate-400" />
                  <span className="text-xs text-slate-400 font-medium">{t('app.scopeLabel')}</span>
                  <select
                    value={scopeFilter}
                    onChange={(e) => {
                      setScopeFilter(e.target.value);
                      setPage(1);
                    }}
                    aria-label={t('app.scopeAria')}
                    className="bg-transparent text-xs font-semibold text-slate-200 focus:outline-none cursor-pointer"
                  >
                    <option value="All" className="bg-slate-900">{t('app.scopeAll')}</option>
                    <option value="Worldwide" className="bg-slate-900">{t('app.scopeWorldwide')}</option>
                    <option value="Europe" className="bg-slate-900">{t('app.scopeEurope')}</option>
                    <option value="Remote" className="bg-slate-900">{t('app.scopeRemote')}</option>
                  </select>
                </div>

                {/* Score Min Filter */}
                <div className="flex items-center gap-1.5 bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 shrink-0">
                  <Sparkles className="h-3.5 w-3.5 text-amber-400" />
                  <span className="text-xs text-slate-400 font-medium">{t('app.matchLabel')}</span>
                  <select
                    value={minScoreFilter ?? ''}
                    onChange={(e) => {
                      setMinScoreFilter(e.target.value ? Number(e.target.value) : undefined);
                      setPage(1);
                    }}
                    aria-label={t('app.matchAria')}
                    className="bg-transparent text-xs font-semibold text-slate-200 focus:outline-none cursor-pointer"
                  >
                    <option value="" className="bg-slate-900">{t('app.matchAll')}</option>
                    <option value="90" className="bg-slate-900">{t('app.match90')}</option>
                    <option value="75" className="bg-slate-900">{t('app.match75')}</option>
                    <option value="60" className="bg-slate-900">{t('app.match60')}</option>
                  </select>
                </div>

              </div>

              {/* Row 2: Detailed Quick Filters */}
              <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-slate-800/80 text-xs">
                
                <div className="flex flex-wrap items-center gap-2">
                  
                  {/* Source Filter */}
                  <div className="flex items-center gap-1.5 bg-slate-800/60 px-2.5 py-1 rounded-lg border border-slate-700/60">
                    <span className="text-slate-400">{t('app.sourceLabel')}</span>
                    <select
                      value={sourceFilter}
                      onChange={(e) => {
                        setSourceFilter(e.target.value);
                        setPage(1);
                      }}
                      aria-label={t('app.sourceAria')}
                      className="bg-transparent text-slate-200 font-semibold focus:outline-none cursor-pointer"
                    >
                      <option value="All" className="bg-slate-900">{t('app.sourceAll')}</option>
                      <option value="HackerNews" className="bg-slate-900">{t('app.sourceHn')}</option>
                      <option value="Remotive" className="bg-slate-900">{t('app.sourceRemotive')}</option>
                      <option value="Jobicy" className="bg-slate-900">{t('app.sourceJobicy')}</option>
                      <option value="CustomAts" className="bg-slate-900">{t('app.sourceCustomAts')}</option>
                    </select>
                  </div>

                  {/* Contract Type Filter */}
                  <div className="flex items-center gap-1.5 bg-slate-800/60 px-2.5 py-1 rounded-lg border border-slate-700/60">
                    <span className="text-slate-400">{t('app.contractLabel')}</span>
                    <select
                      value={contractFilter}
                      onChange={(e) => {
                        setContractFilter(e.target.value);
                        setPage(1);
                      }}
                      aria-label={t('app.contractAria')}
                      className="bg-transparent text-slate-200 font-semibold focus:outline-none cursor-pointer"
                    >
                      <option value="All" className="bg-slate-900">{t('app.contractAll')}</option>
                      <option value="CDI" className="bg-slate-900">{t('app.contractCdi')}</option>
                      <option value="Freelance" className="bg-slate-900">{t('app.contractFreelance')}</option>
                      <option value="partiel" className="bg-slate-900">{t('app.contractPartTime')}</option>
                    </select>
                  </div>

                  {/* Location / EOR Compatible Toggle */}
                  <label className="flex items-center gap-2 bg-slate-800/60 hover:bg-slate-800 px-2.5 py-1 rounded-lg border border-slate-700/60 cursor-pointer text-slate-300">
                    <input
                      type="checkbox"
                      checked={franceOnly}
                      onChange={(e) => {
                        setFranceOnly(e.target.checked);
                        setPage(1);
                      }}
                      className="rounded bg-slate-900 border-slate-700 text-emerald-500 focus:ring-0"
                    />
                    <span>{t('app.compatibleToggle')}</span>
                  </label>

                </div>

                {/* Total count indicator */}
                <div className="text-slate-400 font-medium">
                  <strong className="text-slate-200">{totalJobs}</strong> {t('app.opportunitiesFound')}
                </div>

              </div>

            </div>

            {/* Jobs Grid */}
            {isLoadingJobs ? (
              <div className="py-24 text-center space-y-3">
                <RotateCw className="h-8 w-8 text-emerald-400 animate-spin mx-auto" />
                <p className="text-xs text-slate-400">{t('app.loadingRadar')}</p>
              </div>
            ) : jobs.length === 0 ? (
              <div className="bg-slate-900/40 border border-slate-800 rounded-2xl p-12 text-center space-y-4 max-w-md mx-auto">
                <div className="h-12 w-12 rounded-full bg-slate-800 flex items-center justify-center mx-auto text-slate-400">
                  <Search className="h-6 w-6" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">{t('app.emptyTitle')}</h3>
                  <p className="text-xs text-slate-400 mt-1">
                    {t('app.emptySubtitle')}
                  </p>
                </div>
                <button
                  onClick={handleSync}
                  className="text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-4 py-2 rounded-lg transition"
                >
                  {t('app.scanNewSources')}
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
                {jobs.map((job) => (
                  <JobCard
                    key={job.id}
                    job={job}
                    onViewDetails={(j) => setSelectedJobForDetail(j)}
                    onOpenPitch={(j) => setSelectedJobForPitch(j)}
                    onUpdateStatus={handleUpdateStatus}
                  />
                ))}
              </div>
            )}

            {/* Pagination Controls */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-4 border-t border-slate-800 text-xs text-slate-400">
                <div>
                  {t('app.pageOf', { page, totalPages })}
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page <= 1}
                    className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed text-slate-200 transition"
                  >
                    <ChevronLeft className="h-4 w-4" />
                    <span>{t('app.prev')}</span>
                  </button>

                  <button
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    disabled={page >= totalPages}
                    className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed text-slate-200 transition"
                  >
                    <span>{t('app.next')}</span>
                    <ChevronRight className="h-4 w-4" />
                  </button>
                </div>
              </div>
            )}

          </div>
        )}

      </main>

      {/* Modals */}
      <JobDetailModal
        job={selectedJobForDetail}
        onClose={() => setSelectedJobForDetail(null)}
        onOpenPitch={(j) => setSelectedJobForPitch(j)}
        onUpdateStatus={handleUpdateStatus}
        onSaveNotes={handleSaveNotes}
        onReMatch={handleReMatch}
      />

      <PitchModal
        job={selectedJobForPitch}
        onClose={() => setSelectedJobForPitch(null)}
        activeProfileId={activeProfile?.id}
      />

      <ProfileManagerModal
        isOpen={isProfilesOpen}
        onClose={() => setIsProfilesOpen(false)}
        profiles={profiles}
        activeProfile={activeProfile}
        onRefreshProfiles={loadProfilesAndStats}
      />

      <SettingsModal
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
      />

      <IngestUrlModal
        isOpen={isIngestOpen}
        onClose={() => setIsIngestOpen(false)}
        onSuccess={() => {
          loadProfilesAndStats();
          loadJobs();
        }}
      />

    </div>
  );
}

export default App;

