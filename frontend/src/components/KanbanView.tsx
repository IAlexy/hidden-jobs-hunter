import React from 'react';
import { 
  Building2, 
  Send, 
  Mail, 
  ArrowRight,
  ArrowLeft
} from 'lucide-react';
import { JobOffer } from '../types';
import { useI18n } from '../i18n';

interface KanbanViewProps {
  jobs: JobOffer[];
  onViewDetails: (job: JobOffer) => void;
  onOpenPitch: (job: JobOffer) => void;
  onUpdateStatus: (id: number, status: JobOffer['status']) => void;
}

const COLUMNS: { id: JobOffer['status']; titleKey: string; color: string; badgeBg: string }[] = [
  { id: 'Saved', titleKey: 'kanban.colSaved', color: 'border-blue-500/50', badgeBg: 'bg-blue-500/20 text-blue-300' },
  { id: 'Applied', titleKey: 'kanban.colApplied', color: 'border-amber-500/50', badgeBg: 'bg-amber-500/20 text-amber-300' },
  { id: 'Interview', titleKey: 'kanban.colInterview', color: 'border-emerald-500/50', badgeBg: 'bg-emerald-500/20 text-emerald-300' },
  { id: 'Offer', titleKey: 'kanban.colOffer', color: 'border-purple-500/50', badgeBg: 'bg-purple-500/20 text-purple-300' },
  { id: 'Archived', titleKey: 'kanban.colArchived', color: 'border-slate-700/50', badgeBg: 'bg-slate-800 text-slate-400' },
];

export const KanbanView: React.FC<KanbanViewProps> = ({
  jobs,
  onViewDetails,
  onOpenPitch,
  onUpdateStatus,
}) => {
  const { t } = useI18n();

  return (
    <div className="overflow-x-auto pb-6">
      <div className="flex gap-4 min-w-[1100px]">
        {COLUMNS.map((col) => {
          const colJobs = jobs.filter((j) => j.status === col.id);

          return (
            <div
              key={col.id}
              className={`flex-1 min-w-[260px] max-w-[320px] bg-slate-900/60 border ${col.color} rounded-xl p-3 flex flex-col max-h-[calc(100vh-230px)] shadow-sm`}
            >
              {/* Column Header */}
              <div className="flex items-center justify-between pb-3 mb-3 border-b border-slate-800">
                <span className="text-xs font-bold text-slate-200">{t(col.titleKey)}</span>
                <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full ${col.badgeBg}`}>
                  {colJobs.length}
                </span>
              </div>

              {/* Cards Container */}
              <div className="space-y-3 overflow-y-auto flex-1 pr-1">
                {colJobs.length === 0 ? (
                  <div className="text-center py-8 text-xs text-slate-500 italic">
                    {t('kanban.emptyCol')}
                  </div>
                ) : (
                  colJobs.map((job) => {
                    const match = job.activeMatch;
                    const score = match?.matchScore ?? 0;

                    return (
                      <div
                        key={job.id}
                        className="bg-slate-850 hover:bg-slate-800/90 border border-slate-700/60 rounded-xl p-3.5 shadow-sm transition space-y-2.5 text-xs text-slate-300 group"
                      >
                        <div className="flex items-start justify-between gap-1.5">
                          <span className="text-[10px] font-semibold px-1.5 py-0.2 rounded bg-slate-800 text-slate-400 border border-slate-700 truncate max-w-[120px]">
                            {job.source}
                          </span>
                          {score > 0 && (
                            <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                              {score}% Match
                            </span>
                          )}
                        </div>

                        <div>
                          <h4 
                            onClick={() => onViewDetails(job)}
                            className="font-bold text-white text-xs hover:text-emerald-400 cursor-pointer line-clamp-2"
                          >
                            {job.title}
                          </h4>
                          <span className="text-[11px] text-slate-400 flex items-center gap-1 mt-0.5">
                            <Building2 className="h-3 w-3" />
                            {job.company}
                          </span>
                        </div>

                        {job.directContact && (
                          <div className="flex items-center gap-1 text-[11px] text-amber-300 font-mono truncate bg-amber-500/10 px-2 py-1 rounded">
                            <Mail className="h-3 w-3 shrink-0" />
                            <span className="truncate">{job.directContact}</span>
                          </div>
                        )}

                        {job.userNotes && (
                          <p className="text-[10px] text-slate-400 italic bg-slate-900/60 p-2 rounded border border-slate-800 line-clamp-2">
                            📝 {job.userNotes}
                          </p>
                        )}

                        {/* Actions */}
                        <div className="pt-2 border-t border-slate-800 flex items-center justify-between gap-1">
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => onOpenPitch(job)}
                              title={t('card.pitchTitle')}
                              className="p-1 text-emerald-400 hover:bg-emerald-950/40 rounded transition"
                            >
                              <Send className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => onViewDetails(job)}
                              title={t('card.detailsBtn')}
                              className="text-[10px] text-slate-400 hover:text-white px-1.5 py-0.5 rounded hover:bg-slate-700"
                            >
                              {t('card.detailsBtn')}
                            </button>
                          </div>

                          {/* Move Column Arrows */}
                          <div className="flex items-center gap-1">
                            {col.id !== 'Saved' && (
                              <button
                                onClick={() => {
                                  const prevIdx = COLUMNS.findIndex((c) => c.id === col.id) - 1;
                                  if (prevIdx >= 0) onUpdateStatus(job.id, COLUMNS[prevIdx].id);
                                }}
                                title={t('kanban.moveLeft')}
                                className="p-1 text-slate-400 hover:text-white hover:bg-slate-700 rounded transition"
                              >
                                <ArrowLeft className="h-3 w-3" />
                              </button>
                            )}

                            {col.id !== 'Archived' && (
                              <button
                                onClick={() => {
                                  const nextIdx = COLUMNS.findIndex((c) => c.id === col.id) + 1;
                                  if (nextIdx < COLUMNS.length) onUpdateStatus(job.id, COLUMNS[nextIdx].id);
                                }}
                                title={t('kanban.moveRight')}
                                className="p-1 text-emerald-400 hover:text-emerald-300 hover:bg-emerald-950/40 rounded transition"
                              >
                                <ArrowRight className="h-3 w-3" />
                              </button>
                            )}
                          </div>
                        </div>

                      </div>
                    );
                  })
                )}
              </div>

            </div>
          );
        })}
      </div>
    </div>
  );
};
