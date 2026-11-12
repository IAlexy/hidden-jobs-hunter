import React, { useState, useEffect } from 'react';
import { 
  X, 
  Settings, 
  Cpu, 
  Check, 
  RotateCw, 
  Key, 
  Server,
  Sparkles,
  Save
} from 'lucide-react';
import { AppSettings } from '../types';
import { fetchSettings, updateSettings, testAiConnection } from '../api';
import { useI18n } from '../i18n';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({ isOpen, onClose }) => {
  const { t } = useI18n();
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [geminiKeyInput, setGeminiKeyInput] = useState('');
  const [openAiKeyInput, setOpenAiKeyInput] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    fetchSettings().then((data) => {
      setSettings(data);
      if (data.geminiApiKey) setGeminiKeyInput(data.geminiApiKey);
      if (data.openAiApiKey) setOpenAiKeyInput(data.openAiApiKey);
    });
  }, [isOpen]);

  if (!isOpen || !settings) return null;

  const handleSave = async () => {
    setIsSaving(true);
    try {
      const payload: Partial<AppSettings> = {
        aiProvider: settings.aiProvider,
        geminiApiKey: geminiKeyInput,
        geminiModel: settings.geminiModel,
        openAiApiKey: openAiKeyInput,
        openAiModel: settings.openAiModel,
        ollamaEndpoint: settings.ollamaEndpoint,
        ollamaModel: settings.ollamaModel,
        autoMatchOnSync: settings.autoMatchOnSync,
        onlyFranceCompatibleJobs: settings.onlyFranceCompatibleJobs,
      };
      const updated = await updateSettings(payload);
      setSettings(updated);
      setTestResult({ success: true, message: t('settings.savedSuccess') });
      setTimeout(() => setTestResult(null), 3000);
    } catch {
      alert('Erreur lors de l\'enregistrement des paramètres.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleTestConnection = async () => {
    setIsTesting(true);
    setTestResult(null);
    try {
      let keyToTest = '';
      if (settings.aiProvider === 'Gemini') keyToTest = geminiKeyInput;
      if (settings.aiProvider === 'OpenAI') keyToTest = openAiKeyInput;

      const res = await testAiConnection({
        provider: settings.aiProvider,
        apiKey: keyToTest,
        model: settings.aiProvider === 'Gemini' ? settings.geminiModel : settings.openAiModel,
        endpoint: settings.ollamaEndpoint,
      });
      setTestResult(res);
    } catch (err: any) {
      setTestResult({ success: false, message: err.message || 'Échec du test de connexion' });
    } finally {
      setIsTesting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm overflow-y-auto">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-2xl w-full flex flex-col shadow-2xl overflow-hidden my-auto animate-in fade-in zoom-in-95 duration-150">
        
        {/* Header */}
        <div className="p-5 border-b border-slate-800 flex items-center justify-between gap-4 bg-slate-900/90">
          <div className="flex items-center gap-2.5">
            <div className="h-9 w-9 rounded-xl bg-slate-800 flex items-center justify-center border border-slate-700">
              <Settings className="h-5 w-5 text-slate-300" />
            </div>
            <div>
              <h3 className="font-bold text-white text-base">{t('settings.title')}</h3>
              <p className="text-xs text-slate-400">
                {t('settings.subtitle')}
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
        <div className="p-6 space-y-6 text-xs text-slate-300 overflow-y-auto max-h-[75vh]">
          
          {/* Provider Selection */}
          <div className="space-y-3">
            <label className="text-xs font-bold text-slate-200 uppercase tracking-wider block">
              {t('settings.providerLabel')}
            </label>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              
              {/* Option 1: Heuristic */}
              <div
                onClick={() => setSettings({ ...settings, aiProvider: 'Heuristic' })}
                className={`p-3.5 rounded-xl border cursor-pointer transition flex flex-col justify-between ${
                  settings.aiProvider === 'Heuristic'
                    ? 'bg-emerald-950/20 border-emerald-500/50 shadow-sm'
                    : 'bg-slate-950/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-bold text-white text-xs flex items-center gap-1.5">
                      <Cpu className="h-4 w-4 text-emerald-400" />
                      {t('settings.heuristicTitle')}
                    </span>
                    {settings.aiProvider === 'Heuristic' && (
                      <Check className="h-4 w-4 text-emerald-400" />
                    )}
                  </div>
                  <p className="text-[11px] text-slate-400">
                    {t('settings.heuristicDesc')}
                  </p>
                </div>
              </div>

              {/* Option 2: Gemini */}
              <div
                onClick={() => setSettings({ ...settings, aiProvider: 'Gemini' })}
                className={`p-3.5 rounded-xl border cursor-pointer transition flex flex-col justify-between ${
                  settings.aiProvider === 'Gemini'
                    ? 'bg-emerald-950/20 border-emerald-500/50 shadow-sm'
                    : 'bg-slate-950/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-bold text-white text-xs flex items-center gap-1.5">
                      <Sparkles className="h-4 w-4 text-indigo-400" />
                      {t('settings.geminiTitle')}
                    </span>
                    {settings.aiProvider === 'Gemini' && (
                      <Check className="h-4 w-4 text-emerald-400" />
                    )}
                  </div>
                  <p className="text-[11px] text-slate-400">
                    {t('settings.geminiDesc')}
                  </p>
                </div>
              </div>

              {/* Option 3: Ollama */}
              <div
                onClick={() => setSettings({ ...settings, aiProvider: 'Ollama' })}
                className={`p-3.5 rounded-xl border cursor-pointer transition flex flex-col justify-between ${
                  settings.aiProvider === 'Ollama'
                    ? 'bg-emerald-950/20 border-emerald-500/50 shadow-sm'
                    : 'bg-slate-950/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-bold text-white text-xs flex items-center gap-1.5">
                      <Server className="h-4 w-4 text-amber-400" />
                      {t('settings.ollamaTitle')}
                    </span>
                    {settings.aiProvider === 'Ollama' && (
                      <Check className="h-4 w-4 text-emerald-400" />
                    )}
                  </div>
                  <p className="text-[11px] text-slate-400">
                    {t('settings.ollamaDesc')}
                  </p>
                </div>
              </div>

              {/* Option 4: OpenAI */}
              <div
                onClick={() => setSettings({ ...settings, aiProvider: 'OpenAI' })}
                className={`p-3.5 rounded-xl border cursor-pointer transition flex flex-col justify-between ${
                  settings.aiProvider === 'OpenAI'
                    ? 'bg-emerald-950/20 border-emerald-500/50 shadow-sm'
                    : 'bg-slate-950/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-bold text-white text-xs flex items-center gap-1.5">
                      <Key className="h-4 w-4 text-teal-400" />
                      {t('settings.openaiTitle')}
                    </span>
                    {settings.aiProvider === 'OpenAI' && (
                      <Check className="h-4 w-4 text-emerald-400" />
                    )}
                  </div>
                  <p className="text-[11px] text-slate-400">
                    {t('settings.openaiDesc')}
                  </p>
                </div>
              </div>

            </div>
          </div>

          {/* Conditional Provider Settings */}
          {settings.aiProvider === 'Gemini' && (
            <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-4 space-y-3">
              <h4 className="font-semibold text-slate-200 text-xs">{t('settings.geminiConfig')}</h4>
              <div>
                <label className="block text-slate-400 mb-1">Clé API Google Gemini :</label>
                <input
                  type="password"
                  value={geminiKeyInput}
                  onChange={(e) => setGeminiKeyInput(e.target.value)}
                  placeholder="AIzaSy..."
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-indigo-500"
                />
              </div>
              <div>
                <label className="block text-slate-400 mb-1">Modèle :</label>
                <input
                  type="text"
                  value={settings.geminiModel}
                  onChange={(e) => setSettings({ ...settings, geminiModel: e.target.value })}
                  placeholder="gemini-2.5-flash"
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-indigo-500"
                />
              </div>
            </div>
          )}

          {settings.aiProvider === 'OpenAI' && (
            <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-4 space-y-3">
              <h4 className="font-semibold text-slate-200 text-xs">Configuration OpenAI</h4>
              <div>
                <label className="block text-slate-400 mb-1">Clé API OpenAI :</label>
                <input
                  type="password"
                  value={openAiKeyInput}
                  onChange={(e) => setOpenAiKeyInput(e.target.value)}
                  placeholder="sk-..."
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-teal-500"
                />
              </div>
              <div>
                <label className="block text-slate-400 mb-1">Modèle :</label>
                <input
                  type="text"
                  value={settings.openAiModel}
                  onChange={(e) => setSettings({ ...settings, openAiModel: e.target.value })}
                  placeholder="gpt-4o-mini"
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-teal-500"
                />
              </div>
            </div>
          )}

          {settings.aiProvider === 'Ollama' && (
            <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-4 space-y-3">
              <h4 className="font-semibold text-slate-200 text-xs">Configuration Ollama Local</h4>
              <div>
                <label className="block text-slate-400 mb-1">Endpoint HTTP Ollama :</label>
                <input
                  type="text"
                  value={settings.ollamaEndpoint}
                  onChange={(e) => setSettings({ ...settings, ollamaEndpoint: e.target.value })}
                  placeholder="http://localhost:11434"
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-amber-500"
                />
              </div>
              <div>
                <label className="block text-slate-400 mb-1">Modèle local (ex: llama3, mistral) :</label>
                <input
                  type="text"
                  value={settings.ollamaModel}
                  onChange={(e) => setSettings({ ...settings, ollamaModel: e.target.value })}
                  placeholder="llama3"
                  className="w-full bg-slate-900 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-amber-500"
                />
              </div>
            </div>
          )}

          {/* Sync & Radar Preferences */}
          <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-4 space-y-3">
            <h4 className="font-semibold text-slate-200 text-xs uppercase tracking-wider">Options du Radar</h4>
            
            <label className="flex items-center gap-2 text-slate-300 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.autoMatchOnSync}
                onChange={(e) => setSettings({ ...settings, autoMatchOnSync: e.target.checked })}
                className="rounded bg-slate-900 border-slate-700 text-emerald-500 focus:ring-0"
              />
              <span>Calculer automatiquement le Match IA sur chaque nouvelle offre importée</span>
            </label>

            <label className="flex items-center gap-2 text-slate-300 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.onlyFranceCompatibleJobs}
                onChange={(e) => setSettings({ ...settings, onlyFranceCompatibleJobs: e.target.checked })}
                className="rounded bg-slate-900 border-slate-700 text-emerald-500 focus:ring-0"
              />
              <span>Ignorer immédiatement les offres restreintes strictement aux résidents US/Canada</span>
            </label>
          </div>

          {/* Test connection alert */}
          {testResult && (
            <div
              className={`p-3 rounded-lg border text-xs flex items-center gap-2 ${
                testResult.success
                  ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-300'
                  : 'bg-red-950/40 border-red-500/40 text-red-300'
              }`}
            >
              <span>{testResult.message}</span>
            </div>
          )}

        </div>

        {/* Footer */}
        <div className="p-4 border-t border-slate-800 bg-slate-900/90 flex items-center justify-between gap-3">
          <button
            onClick={handleTestConnection}
            disabled={isTesting}
            className="flex items-center gap-1.5 text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-200 px-3.5 py-2 rounded-lg border border-slate-700 transition"
          >
            <RotateCw className={`h-3.5 w-3.5 ${isTesting ? 'animate-spin' : ''}`} />
            <span>{isTesting ? t('settings.testing') : t('settings.testConnectionBtn')}</span>
          </button>

          <div className="flex items-center gap-2">
            <button
              onClick={onClose}
              className="text-xs font-medium text-slate-400 hover:text-slate-200 px-3 py-2 rounded-lg transition"
            >
              {t('settings.closeBtn')}
            </button>

            <button
              onClick={handleSave}
              disabled={isSaving}
              className="flex items-center gap-2 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-4 py-2 rounded-lg transition shadow-sm"
            >
              <Save className="h-4 w-4" />
              <span>{isSaving ? t('settings.saving') : t('settings.saveBtn')}</span>
            </button>
          </div>
        </div>

      </div>
    </div>
  );
};
