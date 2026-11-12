import React from 'react';
import { 
  Building2, 
  MapPin, 
  Globe, 
  Mail, 
  ExternalLink, 
  Sparkles, 
  Send, 
  CheckCircle2
} from 'lucide-react';
import { JobOffer } from '../types';
import { useI18n } from '../i18n';

interface JobCardProps {
  job: JobOffer;
  onViewDetails: (job: JobOffer) => void;
  onOpenPitch: (job: JobOffer) => void;
  onUpdateStatus: (id: number, status: JobOffer['status']) => void;
}

export const JobCard: React.FC<JobCardProps> = ({
  job,
  onViewDetails,
  onOpenPitch,
  onUpdateStatus,
}) => {
  const { t } = useI18n();
  const match = job.activeMatch;
  const matchScore = match?.matchScore ?? 0;

  // Score color helper
  const getScoreColor = (score: number) => {
    if (score >= 85) return 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40';
    if (score >= 70) return 'bg-amber-500/20 text-amber-300 border-amber-500/40';
    return 'bg-slate-700/40 text-slate-400 border-slate-600/40';
  };

  const getSourceBadge = (source: string) => {
    if (source.includes('HackerNews')) {
      return (
        <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-md bg-orange-500/20 text-orange-300 border border-orange-500/30">
          <Sparkles className="h-3 w-3" />
          {t('card.hnBadge')}
        </span>
      );
    }
    if (source.includes('CustomAts')) {
      return (
        <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-md bg-purple-500/20 text-purple-300 border border-purple-500/30">
          {t('card.atsBadge')}
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-md bg-slate-800 text-slate-300 border border-slate-700">
        {source}
      </span>
    );
  };

  // Parse strengths
  let strengths: string[] = [];
  if (match?.strengthsJson) {
    try {
      strengths = JSON.parse(match.strengthsJson);
    } catch {}
  }

  const tagsList = job.tags ? job.tags.split(',').map((t) => t.trim()).filter(Boolean) : [];

  return (
    <div className="group relative bg-slate-900/70 hover:bg-slate-900 border border-slate-800 hover:border-slate-700/80 rounded-xl p-5 transition-all shadow-sm hover:shadow-md flex flex-col justify-between">
      
      {/* Top Header: Source, Remote Scope & Match Score */}
      <div>
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="flex flex-wrap items-center gap-2">
            {getSourceBadge(job.source)}

            <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-md bg-blue-500/15 text-blue-300 border border-blue-500/25">
              <Globe className="h-3 w-3" />
              {job.remoteScope}
            </span>

            {job.isFranceCompatible && (
              <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-md bg-teal-500/15 text-teal-300 border border-teal-500/25">
                <CheckCircle2 className="h-3 w-3" />
                {t('card.compatibleBadge')}
              </span>
            )}

            <span className="text-[11px] text-slate-400 bg-slate-800/80 px-2 py-0.5 rounded-md border border-slate-700/50">
              {job.contractType}
            </span>
          </div>

          {/* Match Score Badge */}
          {matchScore > 0 && (
            <div
              className={`flex items-center gap-1 text-xs font-bold px-2.5 py-1 rounded-lg border shadow-sm shrink-0 ${getScoreColor(
                matchScore
              )}`}
              title={`Score global: ${matchScore}% (Tech: ${match?.techStackFit}%, Remote: ${match?.remoteEligibilityFit}%)`}
            >
              <Sparkles className="h-3.5 w-3.5" />
              <span>{matchScore}% Match</span>
            </div>
          )}
        </div>

        {/* Title & Company */}
        <div className="mb-2.5">
          <h2 
            onClick={() => onViewDetails(job)}
            className="text-base font-semibold text-slate-100 group-hover:text-emerald-400 cursor-pointer transition line-clamp-2"
          >
            {job.title}
          </h2>
          <div className="flex flex-wrap items-center gap-y-1 gap-x-3 text-xs text-slate-400 mt-1">
            <span className="flex items-center gap-1 font-medium text-slate-300">
              <Building2 className="h-3.5 w-3.5 text-slate-400" />
              {job.company}
            </span>
            <span className="flex items-center gap-1">
              <MapPin className="h-3.5 w-3.5 text-slate-500" />
              {job.location}
            </span>
            {job.salary && (
              <span className="text-emerald-400 font-semibold bg-emerald-950/40 px-2 py-0.5 rounded border border-emerald-800/50">
                💰 {job.salary}
              </span>
            )}
          </div>
        </div>

        {/* Direct Contact Alert if present */}
        {job.directContact && (
          <div className="mb-3 flex items-center gap-2 text-xs bg-amber-500/10 border border-amber-500/30 text-amber-200 px-3 py-1.5 rounded-lg">
            <Mail className="h-3.5 w-3.5 text-amber-400 shrink-0" />
            <span className="truncate">
              {t('card.directContact')} <strong>{job.directContact}</strong>
            </span>
          </div>
        )}

        {/* Description snippet preview */}
        <p className="text-xs text-slate-400 line-clamp-2 mb-3 leading-relaxed">
          {job.descriptionPreview || job.description}
        </p>

        {/* AI Key Strengths Highlight */}
        {strengths.length > 0 && (
          <div className="mb-3 text-[11px] bg-emerald-950/20 border border-emerald-800/30 rounded-lg p-2.5 space-y-1">
            <div className="font-semibold text-emerald-400 flex items-center gap-1">
              <Sparkles className="h-3 w-3" />
              <span>{t('card.whyMatches')}</span>
            </div>
            <ul className="text-slate-300 space-y-0.5">
              {strengths.slice(0, 2).map((s, idx) => (
                <li key={idx} className="flex items-start gap-1.5">
                  <span className="text-emerald-400 mt-0.5">•</span>
                  <span className="line-clamp-1">{s}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Tags */}
        {tagsList.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-4">
            {tagsList.slice(0, 5).map((tag, idx) => (
              <span
                key={idx}
                className="text-[10px] font-medium px-2 py-0.5 rounded bg-slate-800/80 text-slate-300 border border-slate-700/60"
              >
                {tag}
              </span>
            ))}
            {tagsList.length > 5 && (
              <span className="text-[10px] text-slate-500 px-1.5 py-0.5">
                +{tagsList.length - 5}
              </span>
            )}
          </div>
        )}
      </div>

      {/* Card Footer Actions */}
      <div className="pt-3 border-t border-slate-800/80 flex items-center justify-between gap-2">
        
        {/* Status Dropdown */}
        <div className="flex items-center gap-1.5">
          <select
            value={job.status}
            onChange={(e) => onUpdateStatus(job.id, e.target.value as JobOffer['status'])}
            aria-label={t('card.statusAria')}
            className="text-[11px] font-medium bg-slate-800 hover:bg-slate-700/80 text-slate-300 border border-slate-700 rounded-md px-2 py-1 focus:outline-none cursor-pointer"
          >
            <option value="Discovered">{t('status.Discovered')}</option>
            <option value="Saved">{t('status.Saved')}</option>
            <option value="Applied">{t('status.Applied')}</option>
            <option value="Interview">{t('status.Interview')}</option>
            <option value="Offer">{t('status.Offer')}</option>
            <option value="Archived">{t('status.Archived')}</option>
          </select>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2">
          {/* Pitch Generator Button */}
          <button
            onClick={() => onOpenPitch(job)}
            title={t('card.pitchTitle')}
            className="flex items-center gap-1 text-xs font-semibold bg-emerald-600/20 hover:bg-emerald-600/30 text-emerald-300 border border-emerald-500/30 px-2.5 py-1.5 rounded-lg transition"
          >
            <Send className="h-3 w-3" />
            <span>{t('card.pitchBtn')}</span>
          </button>

          {/* Details Button */}
          <button
            onClick={() => onViewDetails(job)}
            className="text-xs font-medium text-slate-400 hover:text-slate-200 bg-slate-800/60 hover:bg-slate-800 px-2.5 py-1.5 rounded-lg border border-slate-700/60 transition"
          >
            {t('card.detailsBtn')}
          </button>

          {/* Direct Link */}
          {job.applyUrl && (
            <a
              href={job.applyUrl}
              target="_blank"
              rel="noreferrer"
              title={t('card.openOriginal')}
              className="p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg border border-slate-700/60 transition"
            >
              <ExternalLink className="h-3.5 w-3.5" />
            </a>
          )}
        </div>

      </div>

    </div>
  );
};
