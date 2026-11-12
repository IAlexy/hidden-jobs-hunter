import React, { useState } from 'react';
import { 
  X, 
  Link2, 
  RotateCw, 
  CheckCircle2, 
  AlertCircle
} from 'lucide-react';
import { ingestAtsUrl } from '../api';
import { useI18n } from '../i18n';

interface IngestUrlModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const IngestUrlModal: React.FC<IngestUrlModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const { t } = useI18n();
  const [url, setUrl] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<{ extractedCount: number; newJobsAdded: number } | null>(null);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleIngest = async () => {
    if (!url.trim()) return;

    setIsLoading(true);
    setError(null);
    setResult(null);

    try {
      const res = await ingestAtsUrl(url.trim());
      setResult({ extractedCount: res.extractedCount, newJobsAdded: res.newJobsAdded });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Échec lors de l\'ingestion');
    } finally {
      setIsLoading(false);
    }
  };

  const sampleUrls = [
    { label: t('ingest.exampleAshby'), url: 'https://jobs.ashbyhq.com/linear' },
    { label: t('ingest.exampleGreenhouse'), url: 'https://boards.greenhouse.io/automattic' },
    { label: t('ingest.exampleLever'), url: 'https://jobs.lever.co/buffer' },
  ];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm overflow-y-auto">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-lg w-full flex flex-col shadow-2xl overflow-hidden my-auto animate-in fade-in zoom-in-95 duration-150">
        
        {/* Header */}
        <div className="p-5 border-b border-slate-800 flex items-center justify-between gap-4 bg-slate-900/90">
          <div className="flex items-center gap-2.5">
            <div className="h-9 w-9 rounded-xl bg-indigo-500/20 flex items-center justify-center border border-indigo-500/30">
              <Link2 className="h-5 w-5 text-indigo-400" />
            </div>
            <div>
              <h3 className="font-bold text-white text-base">{t('ingest.title')}</h3>
              <p className="text-xs text-slate-400">
                {t('ingest.subtitle')}
              </p>
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

        {/* Content */}
        <div className="p-6 space-y-4 text-xs text-slate-300">
          <p className="leading-relaxed">
            {t('ingest.description')}
          </p>

          <div>
            <label className="block text-slate-200 font-semibold mb-1.5">
              {t('ingest.urlLabel')}
            </label>
            <input
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://jobs.ashbyhq.com/nom-startup ou https://boards.greenhouse.io/..."
              className="w-full bg-slate-950 border border-slate-800 rounded-lg p-3 text-xs text-slate-200 focus:outline-none focus:border-indigo-500 font-mono"
            />
          </div>

          {/* Quick presets */}
          <div>
            <span className="text-[11px] text-slate-400 block mb-1">{t('ingest.examplesLabel')}</span>
            <div className="flex flex-wrap gap-1.5">
              {sampleUrls.map((s, idx) => (
                <button
                  key={idx}
                  onClick={() => setUrl(s.url)}
                  className="text-[11px] bg-slate-800 hover:bg-slate-700 text-slate-300 px-2.5 py-1 rounded-md border border-slate-700 transition"
                >
                  {s.label}
                </button>
              ))}
            </div>
          </div>

          {/* Result Alert */}
          {result && (
            <div className="p-3.5 bg-emerald-950/40 border border-emerald-500/40 rounded-xl text-emerald-300 flex items-center gap-2.5">
              <CheckCircle2 className="h-5 w-5 text-emerald-400 shrink-0" />
              <div>
                <strong>{t('ingest.scanSuccessTitle')}</strong>
                <p className="text-[11px] text-slate-300">
                  {t('ingest.scanSuccessStats', { extracted: result.extractedCount, added: result.newJobsAdded })}
                </p>
              </div>
            </div>
          )}

          {error && (
            <div className="p-3.5 bg-red-950/40 border border-red-500/40 rounded-xl text-red-300 flex items-center gap-2.5">
              <AlertCircle className="h-5 w-5 text-red-400 shrink-0" />
              <span>{error}</span>
            </div>
          )}

        </div>

        {/* Footer */}
        <div className="p-4 border-t border-slate-800 bg-slate-900/90 flex items-center justify-end gap-2">
          <button
            onClick={onClose}
            className="text-xs font-medium text-slate-400 hover:text-slate-200 px-3 py-2 rounded-lg transition"
          >
            {t('ingest.closeBtn')}
          </button>

          <button
            onClick={handleIngest}
            disabled={isLoading || !url.trim()}
            className="flex items-center gap-2 text-xs font-semibold bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2 rounded-lg transition shadow-sm disabled:opacity-50"
          >
            <RotateCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            <span>{isLoading ? t('ingest.extracting') : t('ingest.startBtn')}</span>
          </button>
        </div>

      </div>
    </div>
  );
};
