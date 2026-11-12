import React from 'react';
import { 
  Radar, 
  RotateCw, 
  UserCheck, 
  Settings, 
  Layers, 
  Briefcase, 
  Globe2,
  Sparkles,
  Link2
} from 'lucide-react';
import { UserProfile, StatsData } from '../types';

interface HeaderProps {
  activeTab: 'explorer' | 'pipeline';
  setActiveTab: (tab: 'explorer' | 'pipeline') => void;
  activeProfile?: UserProfile;
  profiles: UserProfile[];
  onSelectProfile: (id: number) => void;
  onOpenProfiles: () => void;
  onOpenSettings: () => void;
  onOpenIngest: () => void;
  onSync: () => void;
  isSyncing: boolean;
  stats?: StatsData;
}

export const Header: React.FC<HeaderProps> = ({
  activeTab,
  setActiveTab,
  activeProfile,
  profiles,
  onSelectProfile,
  onOpenProfiles,
  onOpenSettings,
  onOpenIngest,
  onSync,
  isSyncing,
  stats
}) => {
  return (
    <header className="border-b border-slate-800 bg-slate-900/90 backdrop-blur-md sticky top-0 z-30">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3.5">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          
          {/* Logo & Subtitle */}
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-xl bg-gradient-to-tr from-emerald-500 via-teal-500 to-indigo-500 flex items-center justify-center shadow-lg shadow-emerald-500/20">
              <Radar className="h-6 w-6 text-white animate-pulse" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-xl font-bold text-white tracking-tight">HiddenJobs Hunter</h1>
                <span className="text-[10px] uppercase font-semibold px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                  Marché Invisible & Remote
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Détection d'offres sans intermédiaire (HN, ATS direct, APIs) & matching IA depuis la France
              </p>
            </div>
          </div>

          {/* Action Center & Profile Switcher */}
          <div className="flex flex-wrap items-center gap-2.5">
            
            {/* Active Profile Dropdown */}
            <div className="relative flex items-center bg-slate-800/90 border border-slate-700/80 rounded-lg px-2.5 py-1.5 shadow-sm">
              <UserCheck className="h-4 w-4 text-teal-400 mr-2 shrink-0" />
              <div className="flex flex-col text-left mr-2">
                <span className="text-[10px] text-slate-400 font-medium leading-none">Profil actif</span>
                <select
                  value={activeProfile?.id ?? ''}
                  onChange={(e) => onSelectProfile(Number(e.target.value))}
                  aria-label="Sélectionner le profil actif"
                  className="bg-transparent text-xs font-semibold text-slate-200 focus:outline-none cursor-pointer pr-1"
                >
                  {profiles.map((p) => (
                    <option key={p.id} value={p.id} className="bg-slate-800 text-slate-200">
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>
              <button
                onClick={onOpenProfiles}
                title="Gérer les profils et CV"
                className="text-[11px] bg-slate-700/80 hover:bg-slate-700 text-slate-300 px-2 py-1 rounded transition ml-1"
              >
                Gérer
              </button>
            </div>

            {/* Ingest Career ATS URL button */}
            <button
              onClick={onOpenIngest}
              title="Extraire les offres depuis l'URL d'un ATS d'entreprise (Ashby, Greenhouse, Lever)"
              className="flex items-center gap-1.5 text-xs font-medium bg-slate-800 hover:bg-slate-700 text-slate-200 px-3 py-2 rounded-lg border border-slate-700 transition"
            >
              <Link2 className="h-3.5 w-3.5 text-indigo-400" />
              <span>Scraper un ATS</span>
            </button>

            {/* Sync Button */}
            <button
              onClick={onSync}
              disabled={isSyncing}
              className={`flex items-center gap-2 text-xs font-semibold px-3.5 py-2 rounded-lg transition shadow-sm ${
                isSyncing
                  ? 'bg-slate-800 text-slate-400 cursor-not-allowed'
                  : 'bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white shadow-emerald-500/20'
              }`}
            >
              <RotateCw className={`h-3.5 w-3.5 ${isSyncing ? 'animate-spin' : ''}`} />
              <span>{isSyncing ? 'Scan en cours...' : 'Scanner le radar'}</span>
            </button>

            {/* Settings button */}
            <button
              onClick={onOpenSettings}
              title="Paramètres de l'IA et sources"
              className="p-2 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg border border-slate-700/80 transition"
            >
              <Settings className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Navigation Tabs & Stat Highlights */}
        <div className="mt-3.5 pt-3 border-t border-slate-800/80 flex flex-col sm:flex-row items-center justify-between gap-3">
          
          {/* Tabs */}
          <div className="flex items-center gap-1 bg-slate-950/60 p-1 rounded-lg border border-slate-800 self-start sm:self-auto">
            <button
              onClick={() => setActiveTab('explorer')}
              className={`flex items-center gap-2 px-3.5 py-1.5 rounded-md text-xs font-medium transition ${
                activeTab === 'explorer'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <Briefcase className="h-3.5 w-3.5" />
              <span>Radar & Offres trouvées</span>
              {stats && (
                <span className="ml-1 text-[10px] bg-slate-900/60 px-1.5 py-0.2 rounded-full text-slate-200">
                  {stats.totalJobs}
                </span>
              )}
            </button>

            <button
              onClick={() => setActiveTab('pipeline')}
              className={`flex items-center gap-2 px-3.5 py-1.5 rounded-md text-xs font-medium transition ${
                activeTab === 'pipeline'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <Layers className="h-3.5 w-3.5" />
              <span>Pipeline & Suivi</span>
              {stats && (
                <span className="ml-1 text-[10px] bg-slate-900/60 px-1.5 py-0.2 rounded-full text-slate-200">
                  {stats.saved + stats.applied + stats.interview}
                </span>
              )}
            </button>
          </div>

          {/* Stat quick pills */}
          {stats && (
            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
              <div className="flex items-center gap-1 bg-slate-800/50 px-2.5 py-1 rounded-md border border-slate-700/50">
                <Sparkles className="h-3 w-3 text-amber-400" />
                <span>Matches élevés (&gt;75%) :</span>
                <strong className="text-amber-300 font-semibold">{stats.highMatches}</strong>
              </div>

              <div className="flex items-center gap-1 bg-slate-800/50 px-2.5 py-1 rounded-md border border-slate-700/50">
                <Globe2 className="h-3 w-3 text-emerald-400" />
                <span>Éligibles France / Worldwide :</span>
                <strong className="text-emerald-300 font-semibold">{stats.franceCompatible}</strong>
              </div>

              <div className="flex items-center gap-1 bg-slate-800/50 px-2.5 py-1 rounded-md border border-slate-700/50">
                <Radar className="h-3 w-3 text-teal-400" />
                <span>Hacker News directs :</span>
                <strong className="text-teal-300 font-semibold">{stats.hnDirectPosts}</strong>
              </div>
            </div>
          )}
        </div>

      </div>
    </header>
  );
};
