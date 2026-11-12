import React, { useState } from 'react';
import { 
  X, 
  Copy, 
  Check, 
  Sparkles, 
  RotateCw, 
  Mail
} from 'lucide-react';
import { JobOffer } from '../types';
import { generatePitch } from '../api';

interface PitchModalProps {
  job: JobOffer | null;
  onClose: () => void;
  activeProfileId?: number;
}

export const PitchModal: React.FC<PitchModalProps> = ({
  job,
  onClose,
  activeProfileId,
}) => {
  const initialPitch = job?.activeMatch?.tailoredPitch || '';
  const [editedPitch, setEditedPitch] = useState<string | null>(null);
  const [customInstructions, setCustomInstructions] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [copied, setCopied] = useState(false);

  if (!job) return null;

  const pitch = editedPitch ?? initialPitch;

  const handleGenerate = async () => {
    setIsGenerating(true);
    try {
      const newPitch = await generatePitch(job.id, activeProfileId, customInstructions);
      setEditedPitch(newPitch);
    } catch {
      alert('Erreur lors de la génération du message.');
    } finally {
      setIsGenerating(false);
    }
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(pitch);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const mailtoUrl = job.directContact
    ? `mailto:${job.directContact}?subject=${encodeURIComponent(`Application for ${job.title} - Fullstack Software Engineer`)}&body=${encodeURIComponent(pitch)}`
    : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm overflow-y-auto">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-2xl w-full flex flex-col shadow-2xl overflow-hidden my-auto animate-in fade-in zoom-in-95 duration-150">
        
        {/* Header */}
        <div className="p-5 border-b border-slate-800 flex items-center justify-between gap-4 bg-slate-900/90">
          <div className="flex items-center gap-2.5">
            <div className="h-9 w-9 rounded-xl bg-gradient-to-tr from-emerald-500 to-teal-500 flex items-center justify-center shadow-lg shadow-emerald-500/20">
              <Sparkles className="h-5 w-5 text-white" />
            </div>
            <div>
              <h3 className="font-bold text-white text-base">Générateur de Pitch d'Approche Directe</h3>
              <p className="text-xs text-slate-400">
                Message personnalisé pour <strong className="text-slate-200">{job.company}</strong> ({job.title})
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            aria-label="Fermer la modal"
            className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-4">
          
          {/* Direct contact hint */}
          {job.directContact && (
            <div className="flex items-center justify-between text-xs bg-amber-500/10 border border-amber-500/30 text-amber-200 px-3.5 py-2 rounded-lg">
              <span>Destinataire direct identifié : <strong>{job.directContact}</strong></span>
              {mailtoUrl && (
                <a
                  href={mailtoUrl}
                  className="flex items-center gap-1 font-semibold text-amber-300 hover:text-amber-100 underline"
                >
                  <Mail className="h-3.5 w-3.5" />
                  Ouvrir email
                </a>
              )}
            </div>
          )}

          {/* Instructions personnalisées */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">
              Consignes spécifiques pour l'IA (optionnel) :
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={customInstructions}
                onChange={(e) => setCustomInstructions(e.target.value)}
                placeholder="Ex: Rédiger en français, insister sur les microservices C# et proposer 600€ TJM..."
                className="flex-1 bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-emerald-500"
              />
              <button
                onClick={handleGenerate}
                disabled={isGenerating}
                className="flex items-center gap-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-3.5 py-2 rounded-lg transition shrink-0"
              >
                <RotateCw className={`h-3.5 w-3.5 ${isGenerating ? 'animate-spin' : ''}`} />
                <span>{isGenerating ? 'Génération...' : 'Régénérer'}</span>
              </button>
            </div>
          </div>

          {/* Pitch Editor / Viewer */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label className="text-xs font-semibold text-slate-300">
                Message d'accroche (prêt à envoyer) :
              </label>
              <span className="text-[11px] text-slate-400">
                Modifiable directement avant copie
              </span>
            </div>
            <textarea
              value={pitch}
              onChange={(e) => setEditedPitch(e.target.value)}
              rows={12}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl p-4 text-xs font-mono text-slate-200 focus:outline-none focus:border-emerald-500 leading-relaxed resize-y"
            />
          </div>

        </div>

        {/* Footer */}
        <div className="p-4 border-t border-slate-800 bg-slate-900/90 flex items-center justify-between gap-3">
          <span className="text-xs text-slate-400">
            Astuce : les emails courts avec proposition de valeur claire ont 4x plus de réponses.
          </span>

          <div className="flex items-center gap-2">
            <button
              onClick={onClose}
              className="text-xs font-medium text-slate-400 hover:text-slate-200 px-3 py-2 rounded-lg transition"
            >
              Fermer
            </button>

            <button
              onClick={handleCopy}
              className="flex items-center gap-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-4 py-2 rounded-lg transition shadow-sm"
            >
              {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
              <span>{copied ? 'Copié dans le presse-papier !' : 'Copier le message'}</span>
            </button>
          </div>
        </div>

      </div>
    </div>
  );
};
