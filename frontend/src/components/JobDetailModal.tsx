import React, { useState } from 'react';
import { 
  X, 
  Building2, 
  MapPin, 
  Globe, 
  ExternalLink, 
  Mail, 
  Sparkles, 
  CheckCircle2, 
  AlertTriangle, 
  Send, 
  RotateCw,
  Copy,
  Check
} from 'lucide-react';
import { JobOffer } from '../types';
import { useI18n } from '../i18n';

interface JobDetailModalProps {
  job: JobOffer | null;
  onClose: () => void;
  onOpenPitch: (job: JobOffer) => void;
  onUpdateStatus: (id: number, status: JobOffer['status']) => void;
  onSaveNotes: (id: number, notes: string) => Promise<void>;
  onReMatch: (id: number) => Promise<void>;
}

export const JobDetailModal: React.FC<JobDetailModalProps> = ({
  job,
  onClose,
  onOpenPitch,
  onUpdateStatus,
  onSaveNotes,
  onReMatch,
}) => {
  const { t } = useI18n();
  const [notes, setNotes] = useState(job?.userNotes || '');
  const [isSavingNotes, setIsSavingNotes] = useState(false);
  const [isReMatching, setIsReMatching] = useState(false);
  const [copiedContact, setCopiedContact] = useState(false);

  if (!job) return null;

  const match = job.activeMatch;

  let strengths: string[] = [];
  if (match?.strengthsJson) {
    try {
      strengths = JSON.parse(match.strengthsJson);
    } catch {}
  }

  let missingSkills: string[] = [];
  if (match?.missingSkillsJson) {
    try {
      missingSkills = JSON.parse(match.missingSkillsJson);
    } catch {}
  }

  const handleSaveNotes = async () => {
    setIsSavingNotes(true);
    try {
      await onSaveNotes(job.id, notes);
    } finally {
      setIsSavingNotes(false);
    }
  };

  const handleReMatch = async () => {
    setIsReMatching(true);
    try {
      await onReMatch(job.id);
    } finally {
      setIsReMatching(false);
    }
  };

  const handleCopyContact = () => {
    if (job.directContact) {
      navigator.clipboard.writeText(job.directContact);
      setCopiedContact(true);
      setTimeout(() => setCopiedContact(false), 2000);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm overflow-y-auto">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-3xl w-full max-h-[90vh] flex flex-col shadow-2xl overflow-hidden my-auto animate-in fade-in zoom-in-95 duration-150">
        
        {/* Header */}
        <div className="p-6 border-b border-slate-800 flex items-start justify-between gap-4 bg-slate-900/90 sticky top-0 z-10">
          <div>
            <div className="flex flex-wrap items-center gap-2 mb-2">
              <span className="text-xs font-semibold px-2.5 py-0.5 rounded-md bg-slate-800 text-slate-300 border border-slate-700">
                {job.source}
              </span>
              <span className="text-xs font-semibold px-2.5 py-0.5 rounded-md bg-blue-500/20 text-blue-300 border border-blue-500/30">
                {job.remoteScope}
              </span>
              <span className="text-xs font-medium px-2.5 py-0.5 rounded-md bg-slate-800 text-slate-400">
                {job.contractType}
              </span>
              {job.isFranceCompatible && (
                <span className="text-xs font-medium px-2.5 py-0.5 rounded-md bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                  ✓ {t('card.compatibleBadge')}
                </span>
              )}
            </div>
            <h2 className="text-xl font-bold text-white tracking-tight">{job.title}</h2>
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-slate-400 mt-1">
              <span className="flex items-center gap-1.5 font-medium text-slate-200">
                <Building2 className="h-4 w-4 text-slate-400" />
                {job.company}
              </span>
              <span className="flex items-center gap-1.5">
                <MapPin className="h-4 w-4 text-slate-500" />
                {job.location}
              </span>
              {job.salary && (
                <span className="text-emerald-400 font-semibold bg-emerald-950/40 px-2 py-0.5 rounded border border-emerald-800/40">
                  💰 {job.salary}
                </span>
              )}
            </div>
          </div>

          <button
            onClick={onClose}
            aria-label={t('detail.closeAria')}
            className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Content Scrollable */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1 text-slate-300 text-sm">
          
          {/* Direct Contact Banner */}
          {job.directContact && (
            <div className="flex items-center justify-between gap-3 bg-amber-500/10 border border-amber-500/30 p-4 rounded-xl text-amber-200">
              <div className="flex items-center gap-3">
                <div className="h-9 w-9 rounded-lg bg-amber-500/20 flex items-center justify-center shrink-0">
                  <Mail className="h-5 w-5 text-amber-400" />
                </div>
                <div>
                  <h4 className="text-xs font-semibold text-amber-300 uppercase tracking-wider">{t('detail.directContactTitle')}</h4>
                  <p className="text-sm font-mono font-medium text-white">{job.directContact}</p>
                </div>
              </div>
              <button
                onClick={handleCopyContact}
                className="flex items-center gap-1.5 text-xs font-semibold bg-amber-500/20 hover:bg-amber-500/30 text-amber-200 px-3 py-2 rounded-lg border border-amber-500/40 transition"
              >
                {copiedContact ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                <span>{copiedContact ? t('detail.copied') : t('detail.copyEmail')}</span>
              </button>
            </div>
          )}

          {/* AI Matching Breakdown */}
          {match && (
            <div className="bg-slate-950/70 border border-slate-800 rounded-xl p-5 space-y-4">
              <div className="flex items-center justify-between gap-3 pb-3 border-b border-slate-800">
                <div className="flex items-center gap-2.5">
                  <div className="h-8 w-8 rounded-lg bg-emerald-500/20 flex items-center justify-center">
                    <Sparkles className="h-4 w-4 text-emerald-400" />
                  </div>
                  <div>
                    <h3 className="font-bold text-white text-base">{t('detail.aiTitle')}</h3>
                    <p className="text-xs text-slate-400">{t('detail.aiSubtitle')}</p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={handleReMatch}
                    disabled={isReMatching}
                    title={t('detail.rematchTitle')}
                    className="p-1.5 text-xs text-slate-400 hover:text-white bg-slate-800 hover:bg-slate-700 rounded-lg transition"
                  >
                    <RotateCw className={`h-3.5 w-3.5 ${isReMatching ? 'animate-spin' : ''}`} />
                  </button>

                  <div className="px-3 py-1 bg-emerald-500/20 text-emerald-300 font-bold rounded-lg border border-emerald-500/40 text-base">
                    {match.matchScore}% Match
                  </div>
                </div>
              </div>

              {/* Progress bars */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div>
                  <div className="flex justify-between text-slate-300 mb-1">
                    <span>{t('detail.techFit')}</span>
                    <strong className="text-emerald-400">{match.techStackFit}%</strong>
                  </div>
                  <div className="w-full bg-slate-800 h-2 rounded-full overflow-hidden">
                    <div 
                      className="bg-emerald-500 h-full rounded-full transition-all"
                      style={{ width: `${match.techStackFit}%` }}
                    />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-slate-300 mb-1">
                    <span>{t('detail.remoteFit')}</span>
                    <strong className="text-teal-400">{match.remoteEligibilityFit}%</strong>
                  </div>
                  <div className="w-full bg-slate-800 h-2 rounded-full overflow-hidden">
                    <div 
                      className="bg-teal-500 h-full rounded-full transition-all"
                      style={{ width: `${match.remoteEligibilityFit}%` }}
                    />
                  </div>
                </div>
              </div>

              {/* Summary */}
              {match.summary && (
                <p className="text-xs text-slate-300 italic bg-slate-900/80 p-3 rounded-lg border border-slate-800">
                  « {match.summary} »
                </p>
              )}

              {/* Strengths & Missing skills */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {strengths.length > 0 && (
                  <div className="bg-emerald-950/20 border border-emerald-800/30 rounded-lg p-3">
                    <h4 className="text-xs font-semibold text-emerald-400 mb-2 flex items-center gap-1.5">
                      <CheckCircle2 className="h-3.5 w-3.5" />
                      {t('detail.strengthsTitle')}
                    </h4>
                    <ul className="text-xs space-y-1 text-slate-300">
                      {strengths.map((s, i) => (
                        <li key={i} className="flex items-start gap-1.5">
                          <span className="text-emerald-400">•</span>
                          <span>{s}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {missingSkills.length > 0 && (
                  <div className="bg-amber-950/20 border border-amber-800/30 rounded-lg p-3">
                    <h4 className="text-xs font-semibold text-amber-400 mb-2 flex items-center gap-1.5">
                      <AlertTriangle className="h-3.5 w-3.5" />
                      {t('detail.gapsTitle')}
                    </h4>
                    <ul className="text-xs space-y-1 text-slate-300">
                      {missingSkills.map((m, i) => (
                        <li key={i} className="flex items-start gap-1.5">
                          <span className="text-amber-400">•</span>
                          <span>{m}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>

              {/* Administrative Feasibility */}
              {match.administrativeFeasibility && (
                <div className="bg-blue-950/20 border border-blue-800/30 rounded-lg p-3">
                  <h4 className="text-xs font-semibold text-blue-300 mb-1 flex items-center gap-1.5">
                    <Globe className="h-3.5 w-3.5 text-blue-400" />
                    {t('detail.feasibilityTitle')}
                  </h4>
                  <p className="text-xs text-slate-300 leading-relaxed">
                    {match.administrativeFeasibility}
                  </p>
                </div>
              )}

              {/* Tailored Pitch Preview CTA */}
              <div className="pt-2 flex items-center justify-between">
                <span className="text-xs text-slate-400">
                  {t('detail.pitchReadyHint')}
                </span>
                <button
                  onClick={() => onOpenPitch(job)}
                  className="flex items-center gap-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-3 py-1.5 rounded-lg transition shadow-sm"
                >
                  <Send className="h-3.5 w-3.5" />
                  <span>{t('detail.viewPitchBtn')}</span>
                </button>
              </div>

            </div>
          )}

          {/* Job Full Description */}
          <div>
            <h4 className="text-sm font-bold text-white mb-2 uppercase tracking-wider">{t('detail.descriptionTitle')}</h4>
            <div className="bg-slate-950/50 border border-slate-800 rounded-xl p-4 text-xs text-slate-300 whitespace-pre-wrap leading-relaxed max-h-72 overflow-y-auto">
              {job.description || t('detail.noDescription')}
            </div>
          </div>

          {/* User Notes */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h4 className="text-sm font-bold text-white uppercase tracking-wider">{t('detail.notesTitle')}</h4>
              <button
                onClick={handleSaveNotes}
                disabled={isSavingNotes}
                className="text-xs text-emerald-400 hover:text-emerald-300 font-semibold"
              >
                {isSavingNotes ? t('detail.savingNotes') : t('detail.saveNotesBtn')}
              </button>
            </div>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('detail.notesPlaceholder')}
              className="w-full bg-slate-950/50 border border-slate-800 rounded-xl p-3 text-xs text-slate-200 focus:outline-none focus:border-emerald-500 min-h-[80px]"
            />
          </div>

        </div>

        {/* Footer Actions */}
        <div className="p-4 border-t border-slate-800 bg-slate-900/95 flex flex-wrap items-center justify-between gap-3">
          
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-400">{t('detail.statusLabel')}</span>
            <select
              value={job.status}
              onChange={(e) => onUpdateStatus(job.id, e.target.value as JobOffer['status'])}
              aria-label={t('detail.statusAria')}
              className="text-xs font-semibold bg-slate-800 text-slate-200 border border-slate-700 rounded-lg px-3 py-1.5 cursor-pointer focus:outline-none"
            >
              <option value="Discovered">{t('status.Discovered')}</option>
              <option value="Saved">{t('status.Saved')}</option>
              <option value="Applied">{t('status.Applied')}</option>
              <option value="Interview">{t('status.Interview')}</option>
              <option value="Offer">{t('status.Offer')}</option>
              <option value="Archived">{t('status.Archived')}</option>
              <option value="Rejected">{t('status.Rejected')}</option>
            </select>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => onOpenPitch(job)}
              className="flex items-center gap-1.5 text-xs font-semibold bg-emerald-600/20 hover:bg-emerald-600/30 text-emerald-300 border border-emerald-500/30 px-3 py-2 rounded-lg transition"
            >
              <Send className="h-3.5 w-3.5" />
              <span>{t('detail.generatePitchBtn')}</span>
            </button>

            {job.applyUrl && (
              <a
                href={job.applyUrl}
                target="_blank"
                rel="noreferrer"
                className="flex items-center gap-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-3.5 py-2 rounded-lg transition shadow-sm"
              >
                <span>{t('detail.openJobBtn')}</span>
                <ExternalLink className="h-3.5 w-3.5" />
              </a>
            )}
          </div>

        </div>

      </div>
    </div>
  );
};
