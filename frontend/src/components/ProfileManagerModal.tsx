import React, { useState, useEffect } from 'react';
import { 
  X, 
  User, 
  Plus, 
  Sparkles, 
  Check, 
  Trash2, 
  Save, 
  FileText,
  Globe,
  Briefcase
} from 'lucide-react';
import { ProfileTemplate, UserProfile } from '../types';
import { createProfile, updateProfile, deleteProfile, activateProfile, parseCvWithAi, fetchProfileTemplates } from '../api';

const FALLBACK_TEMPLATES: ProfileTemplate[] = [
  {
    id: 'software-dev',
    name: 'Fullstack Software Engineer',
    title: 'Senior Fullstack Software Engineer',
    jobCategory: 'software-dev',
    yearsOfExperience: 5,
    primarySkills: '.NET, C#, React, TypeScript, Node.js, PostgreSQL, REST APIs, GraphQL',
    secondarySkills: 'Docker, AWS, Azure, Next.js, TailwindCSS, Redis, CI/CD',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 65k-85k€ / Freelance: 500-650€ TJM / International: $90k-$140k',
  },
  {
    id: 'data',
    name: 'Data Scientist / AI Engineer',
    title: 'Senior Data Scientist & AI/ML Engineer',
    jobCategory: 'data',
    yearsOfExperience: 5,
    primarySkills: 'Python, PyTorch, TensorFlow, LLM, RAG, Pandas, SQL, Spark, Machine Learning',
    secondarySkills: 'Snowflake, dbt, Airflow, Docker, AWS, MLOps, Hugging Face',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 70k-95k€ / Freelance: 550-750€ TJM / International: $100k-$160k',
  },
  {
    id: 'devops',
    name: 'DevOps / Cloud Architect',
    title: 'Senior DevOps & Cloud Platform Architect',
    jobCategory: 'devops',
    yearsOfExperience: 6,
    primarySkills: 'Kubernetes, Terraform, AWS, GCP, Azure, Docker, CI/CD, Linux, Go',
    secondarySkills: 'Prometheus, Grafana, Ansible, Helm, ArgoCD, Datadog, GitHub Actions',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 70k-90k€ / Freelance: 550-700€ TJM / International: $100k-$150k',
  },
  {
    id: 'product',
    name: 'Product Manager (Tech / SaaS)',
    title: 'Senior Product Manager',
    jobCategory: 'product',
    yearsOfExperience: 5,
    primarySkills: 'Product Discovery, Roadmap, Scrum, Agile, User Research, Product Strategy, SQL, A/B Testing',
    secondarySkills: 'Jira, Notion, Linear, Figma, Amplitude, Mixpanel, Stakeholder Management',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 65k-85k€ / Freelance: 500-650€ TJM / International: $90k-$140k',
  },
  {
    id: 'design',
    name: 'UX/UI Product Designer',
    title: 'Senior UX/UI Product Designer',
    jobCategory: 'design',
    yearsOfExperience: 5,
    primarySkills: 'Figma, Design Systems, UX Research, Prototyping, Wireframing, UI Design, Usability Testing',
    secondarySkills: 'Framer, Webflow, HTML, CSS, Accessibility, Workshop Facilitation',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 55k-75k€ / Freelance: 450-600€ TJM / International: $80k-$125k',
  },
  {
    id: 'marketing',
    name: 'Growth & Digital Marketing',
    title: 'Head of Growth & Product Marketing',
    jobCategory: 'marketing',
    yearsOfExperience: 5,
    primarySkills: 'Growth Hacking, SEO, SEM, HubSpot, Google Analytics, Copywriting, CRM, Demand Generation',
    secondarySkills: 'Product Marketing, Content Strategy, Paid Social, Email Automation, A/B Testing',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 55k-80k€ / Freelance: 450-600€ TJM / International: $80k-$130k',
  },
  {
    id: 'cybersecurity',
    name: 'Cybersecurity / SecOps Engineer',
    title: 'Senior Cybersecurity & SecOps Engineer',
    jobCategory: 'cybersecurity',
    yearsOfExperience: 6,
    primarySkills: 'Pentest, SOC, SIEM, Zero Trust, Cloud Security, AppSec, IAM, Incident Response',
    secondarySkills: 'ISO 27001, SOC2, AWS Security, Kubernetes, Python, Threat Hunting',
    preferredContracts: 'CDI,Freelance/Contractor',
    expectedSalaryOrTjm: 'CDI: 70k-95k€ / Freelance: 600-800€ TJM / International: $100k-$155k',
  },
];

interface ProfileManagerModalProps {
  isOpen: boolean;
  onClose: () => void;
  profiles: UserProfile[];
  activeProfile?: UserProfile;
  onRefreshProfiles: () => Promise<void>;
}

export const ProfileManagerModal: React.FC<ProfileManagerModalProps> = ({
  isOpen,
  onClose,
  profiles,
  activeProfile,
  onRefreshProfiles,
}) => {
  const [selectedProfileId, setSelectedProfileId] = useState<number | null>(activeProfile?.id ?? null);
  const [isCreatingNew, setIsCreatingNew] = useState(false);
  const [isParsingCv, setIsParsingCv] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [templates, setTemplates] = useState<ProfileTemplate[]>(FALLBACK_TEMPLATES);

  // Form State
  const [formData, setFormData] = useState<Partial<UserProfile>>({
    name: activeProfile?.name || '',
    title: activeProfile?.title || 'Fullstack Software Engineer',
    country: activeProfile?.country || 'France',
    jobCategory: activeProfile?.jobCategory || 'software-dev',
    yearsOfExperience: activeProfile?.yearsOfExperience ?? 5,
    preferredContracts: activeProfile?.preferredContracts || 'CDI,Freelance/Contractor',
    remoteScopePreference: activeProfile?.remoteScopePreference || 'Worldwide,Europe,France',
    canWorkAsContractorB2B: activeProfile?.canWorkAsContractorB2B ?? true,
    primarySkills: activeProfile?.primarySkills || '.NET, C#, React, TypeScript, ASP.NET Core',
    secondarySkills: activeProfile?.secondarySkills || 'Docker, Azure, SQL, CI/CD',
    languages: activeProfile?.languages || 'Français (natif), Anglais (bilingue)',
    expectedSalaryOrTjm: activeProfile?.expectedSalaryOrTjm || 'CDI: 65k-85k€ / Freelance: 500-650€ TJM',
    preferredPitchLanguage: activeProfile?.preferredPitchLanguage || 'Auto',
    resumeText: activeProfile?.resumeText || '',
  });

  useEffect(() => {
    if (!isOpen) return;
    let mounted = true;
    fetchProfileTemplates()
      .then((res) => {
        if (mounted && Array.isArray(res) && res.length > 0) {
          setTemplates(res);
        }
      })
      .catch(() => {
        // Fallback templates already loaded
      });
    return () => {
      mounted = false;
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleApplyTemplate = (templateId: string) => {
    const tpl = templates.find((t) => t.id === templateId);
    if (!tpl) return;
    setFormData((prev) => ({
      ...prev,
      name: isCreatingNew || !prev.name ? `Profil ${tpl.name}` : prev.name,
      title: tpl.title,
      jobCategory: tpl.jobCategory,
      yearsOfExperience: tpl.yearsOfExperience,
      primarySkills: tpl.primarySkills,
      secondarySkills: tpl.secondarySkills,
      preferredContracts: tpl.preferredContracts,
      expectedSalaryOrTjm: tpl.expectedSalaryOrTjm,
    }));
  };

  const handleSelectProfile = (p: UserProfile) => {
    setIsCreatingNew(false);
    setSelectedProfileId(p.id);
    setFormData({
      ...p,
    });
  };

  const handleStartNew = () => {
    setIsCreatingNew(true);
    setSelectedProfileId(null);
    setFormData({
      name: 'Nouveau profil (Autre métier / Spécialité)',
      title: 'Software Engineer',
      country: 'France',
      jobCategory: 'software-dev',
      yearsOfExperience: 3,
      preferredContracts: 'CDI,Freelance/Contractor',
      remoteScopePreference: 'Worldwide,Europe,France',
      canWorkAsContractorB2B: true,
      primarySkills: '',
      secondarySkills: '',
      languages: 'Français, Anglais',
      expectedSalaryOrTjm: '',
      preferredPitchLanguage: 'Auto',
      resumeText: '',
    });
  };

  const handleParseCv = async () => {
    if (!formData.resumeText) {
      alert('Veuillez d\'abord coller votre CV ou une description de votre profil.');
      return;
    }

    setIsParsingCv(true);
    try {
      const parsed = await parseCvWithAi(formData.resumeText, formData.name);
      setFormData((prev) => ({
        ...prev,
        ...parsed,
      }));
    } catch {
      alert('Impossible d\'extraire automatiquement le profil.');
    } finally {
      setIsParsingCv(false);
    }
  };

  const handleSave = async () => {
    if (!formData.name) {
      alert('Le nom du profil est requis.');
      return;
    }

    setIsSaving(true);
    try {
      if (isCreatingNew) {
        await createProfile(formData);
      } else if (selectedProfileId) {
        await updateProfile(selectedProfileId, formData);
      }
      await onRefreshProfiles();
      setIsCreatingNew(false);
    } catch {
      alert('Erreur lors de la sauvegarde.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleActivate = async (id: number) => {
    try {
      await activateProfile(id);
      await onRefreshProfiles();
    } catch {
      alert('Erreur lors de l\'activation.');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Supprimer définitivement ce profil ?')) return;
    try {
      await deleteProfile(id);
      await onRefreshProfiles();
      if (selectedProfileId === id) {
        setIsCreatingNew(false);
        setSelectedProfileId(profiles.find((p) => p.id !== id)?.id ?? null);
      }
    } catch {
      alert('Erreur lors de la suppression.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm overflow-y-auto">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-4xl w-full max-h-[92vh] flex flex-col shadow-2xl overflow-hidden my-auto animate-in fade-in zoom-in-95 duration-150">
        
        {/* Header */}
        <div className="p-5 border-b border-slate-800 flex items-center justify-between gap-4 bg-slate-900/90">
          <div className="flex items-center gap-2.5">
            <div className="h-9 w-9 rounded-xl bg-gradient-to-tr from-emerald-500 to-teal-500 flex items-center justify-center">
              <User className="h-5 w-5 text-white" />
            </div>
            <div>
              <h3 className="font-bold text-white text-base">Gestionnaire de Profils & CV</h3>
              <p className="text-xs text-slate-400">
                Configurez votre profil ou ceux de vos amis pour filtrer et matcher les offres
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

        {/* Content Layout */}
        <div className="flex flex-col md:flex-row flex-1 overflow-hidden">
          
          {/* Left Sidebar: Profile List */}
          <div className="w-full md:w-64 border-b md:border-b-0 md:border-r border-slate-800 bg-slate-950/50 p-4 space-y-2 overflow-y-auto shrink-0">
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-slate-400">Profils enregistrés</span>
              <button
                onClick={handleStartNew}
                className="p-1 text-emerald-400 hover:text-emerald-300 hover:bg-emerald-950/40 rounded transition"
                title="Ajouter un nouveau profil"
              >
                <Plus className="h-4 w-4" />
              </button>
            </div>

            {profiles.map((p) => {
              const isSelected = selectedProfileId === p.id && !isCreatingNew;
              return (
                <div
                  key={p.id}
                  onClick={() => handleSelectProfile(p)}
                  className={`group p-3 rounded-xl border transition cursor-pointer flex flex-col justify-between ${
                    isSelected
                      ? 'bg-slate-800/90 border-emerald-500/50 shadow-sm'
                      : 'bg-slate-900/60 border-slate-800/80 hover:bg-slate-800/40'
                  }`}
                >
                  <div className="flex items-start justify-between gap-1 mb-1">
                    <span className="text-xs font-bold text-white truncate">{p.name}</span>
                    {p.isActive && (
                      <span className="text-[10px] bg-emerald-500/20 text-emerald-300 font-semibold px-1.5 py-0.2 rounded border border-emerald-500/30 shrink-0">
                        Actif
                      </span>
                    )}
                  </div>
                  <p className="text-[11px] text-slate-400 truncate mb-2">{p.title}</p>

                  <div className="flex items-center justify-between pt-1 border-t border-slate-800/60 text-[10px]">
                    <span className="text-slate-500">{p.yearsOfExperience} ans exp.</span>

                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition">
                      {!p.isActive && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleActivate(p.id);
                          }}
                          className="text-teal-400 hover:underline"
                        >
                          Activer
                        </button>
                      )}
                      {profiles.length > 1 && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDelete(p.id);
                          }}
                          className="text-red-400 hover:text-red-300 ml-1"
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}

            <button
              onClick={handleStartNew}
              className="w-full flex items-center justify-center gap-1.5 text-xs font-semibold py-2.5 px-3 border border-dashed border-slate-700 hover:border-emerald-500/60 text-slate-300 hover:text-emerald-400 rounded-xl transition"
            >
              <Plus className="h-3.5 w-3.5" />
              <span>Créer un autre profil</span>
            </button>
          </div>

          {/* Right Area: Profile Form */}
          <div className="flex-1 p-6 overflow-y-auto space-y-5">
            
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div>
                <h4 className="font-bold text-white text-base">
                  {isCreatingNew ? 'Nouveau Profil' : `Modifier: ${formData.name}`}
                </h4>
                <p className="text-xs text-slate-400">
                  Définissez vos critères de recherche (métier, compétences, pays de résidence, modalités remote)
                </p>
              </div>

              {!isCreatingNew && selectedProfileId && !activeProfile?.isActive && (
                <button
                  onClick={() => handleActivate(selectedProfileId)}
                  className="flex items-center gap-1 text-xs font-semibold bg-teal-600/20 text-teal-300 border border-teal-500/30 px-3 py-1.5 rounded-lg hover:bg-teal-600/30 transition"
                >
                  <Check className="h-3.5 w-3.5" />
                  <span>Définir comme profil actif</span>
                </button>
              )}
            </div>

            {/* 1-Click Profession Template Selector */}
            <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-3.5 space-y-2.5">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                  <Briefcase className="h-3.5 w-3.5" />
                  <span>Charger un modèle de métier (Template 1-clic) :</span>
                </span>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {templates.map((tpl) => (
                  <button
                    key={tpl.id}
                    type="button"
                    onClick={() => handleApplyTemplate(tpl.id)}
                    className={`text-[11px] font-medium px-2.5 py-1 rounded-lg border transition ${
                      formData.jobCategory === tpl.jobCategory && formData.title === tpl.title
                        ? 'bg-emerald-600/25 border-emerald-500/50 text-emerald-300'
                        : 'bg-slate-900 hover:bg-slate-800 border-slate-800 text-slate-300'
                    }`}
                  >
                    {tpl.name}
                  </button>
                ))}
              </div>
            </div>

            {/* CV Text Area with AI Parse CTA */}
            <div className="bg-slate-950/50 border border-slate-800 rounded-xl p-4 space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold text-slate-200 uppercase tracking-wider flex items-center gap-1.5">
                  <FileText className="h-4 w-4 text-indigo-400" />
                  <span>Coller le CV ou un prompt de description</span>
                </label>
                <button
                  onClick={handleParseCv}
                  disabled={isParsingCv}
                  className="flex items-center gap-1.5 text-xs font-semibold bg-indigo-600 hover:bg-indigo-500 text-white px-3 py-1.5 rounded-lg transition shadow-sm"
                >
                  <Sparkles className={`h-3.5 w-3.5 ${isParsingCv ? 'animate-spin' : ''}`} />
                  <span>{isParsingCv ? 'Analyse en cours...' : 'Extraire avec l\'IA'}</span>
                </button>
              </div>
              <textarea
                value={formData.resumeText || ''}
                onChange={(e) => setFormData({ ...formData, resumeText: e.target.value })}
                placeholder="Collez ici le texte brut de votre CV ou une description complète de votre parcours, réalisations et compétences..."
                rows={5}
                className="w-full bg-slate-900 border border-slate-800 rounded-lg p-3 text-xs text-slate-200 focus:outline-none focus:border-indigo-500"
              />
            </div>

            {/* Form Fields */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
              <div>
                <label className="block text-slate-300 font-semibold mb-1">Nom du profil / Titulaire :</label>
                <input
                  type="text"
                  value={formData.name || ''}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="Ex: Demo Profile (Fullstack .NET / React)"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Titre recherché :</label>
                <input
                  type="text"
                  value={formData.title || ''}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  placeholder="Ex: Senior Fullstack Engineer, Data Scientist, Product Manager..."
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Domaine / Catégorie de métier :</label>
                <select
                  aria-label="Domaine ou catégorie de métier"
                  value={formData.jobCategory || 'software-dev'}
                  onChange={(e) => setFormData({ ...formData, jobCategory: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                >
                  <option value="software-dev">Software Engineering (Fullstack, Backend, Frontend)</option>
                  <option value="data">Data Science, Machine Learning & IA</option>
                  <option value="devops">DevOps, SRE & Cloud Infrastructure</option>
                  <option value="product">Product Management & Agile</option>
                  <option value="design">UX/UI & Product Design</option>
                  <option value="marketing">Growth, Digital & Product Marketing</option>
                  <option value="sales">Sales, Business Development & Account Exec</option>
                  <option value="cybersecurity">Cybersecurity & SecOps</option>
                  <option value="all">Tous domaines confondus</option>
                </select>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Pays de résidence :</label>
                <input
                  type="text"
                  aria-label="Pays de résidence"
                  value={formData.country || 'France'}
                  onChange={(e) => setFormData({ ...formData, country: e.target.value })}
                  placeholder="Ex: France, Canada, Switzerland, Belgium, United Kingdom..."
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Années d'expérience :</label>
                <input
                  type="number"
                  min="0"
                  max="30"
                  value={formData.yearsOfExperience ?? 5}
                  onChange={(e) => setFormData({ ...formData, yearsOfExperience: Number(e.target.value) })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Langues maîtrisées :</label>
                <input
                  type="text"
                  value={formData.languages || ''}
                  onChange={(e) => setFormData({ ...formData, languages: e.target.value })}
                  placeholder="Français (natif), Anglais (bilingue)"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div className="sm:col-span-2">
                <label className="block text-slate-300 font-semibold mb-1">Compétences clés (séparées par virgules) :</label>
                <input
                  type="text"
                  value={formData.primarySkills || ''}
                  onChange={(e) => setFormData({ ...formData, primarySkills: e.target.value })}
                  placeholder=".NET, C#, React, TypeScript, ASP.NET Core, SQL Server, PostgreSQL, REST"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div className="sm:col-span-2">
                <label className="block text-slate-300 font-semibold mb-1">Compétences secondaires / Outils :</label>
                <input
                  type="text"
                  value={formData.secondarySkills || ''}
                  onChange={(e) => setFormData({ ...formData, secondarySkills: e.target.value })}
                  placeholder="Docker, Azure, CI/CD, Next.js, Redis, TailwindCSS"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Contrats acceptés :</label>
                <input
                  type="text"
                  value={formData.preferredContracts || ''}
                  onChange={(e) => setFormData({ ...formData, preferredContracts: e.target.value })}
                  placeholder="CDI,Freelance/Contractor,Temps partiel"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Rémunération / TJM souhaité :</label>
                <input
                  type="text"
                  value={formData.expectedSalaryOrTjm || ''}
                  onChange={(e) => setFormData({ ...formData, expectedSalaryOrTjm: e.target.value })}
                  placeholder="Ex: CDI 70k€ / Freelance 550€ TJM"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-slate-200 focus:border-emerald-500"
                />
              </div>
            </div>

            {/* International Remote & B2B Specifics */}
            <div className="bg-slate-950/60 border border-slate-800 rounded-xl p-3.5 space-y-2 text-xs">
              <span className="font-semibold text-teal-300 flex items-center gap-1.5">
                <Globe className="h-4 w-4" />
                Compatibilité administrative & Remote international ({formData.country || 'France'})
              </span>
              <label className="flex items-center gap-2 text-slate-300 cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.canWorkAsContractorB2B ?? true}
                  onChange={(e) => setFormData({ ...formData, canWorkAsContractorB2B: e.target.checked })}
                  className="rounded bg-slate-900 border-slate-700 text-emerald-500 focus:ring-0"
                />
                <span>
                  <strong>Peut facturer en tant que prestataire B2B (Indépendant / Société) ou via EOR</strong> : permet de travailler légalement pour n'importe quelle entreprise internationale sans qu'elle n'ait besoin d'entité locale dans votre pays.
                </span>
              </label>
            </div>

            {/* Submit Button */}
            <div className="pt-2 flex justify-end">
              <button
                onClick={handleSave}
                disabled={isSaving}
                className="flex items-center gap-2 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white px-5 py-2.5 rounded-xl transition shadow-sm"
              >
                <Save className="h-4 w-4" />
                <span>{isSaving ? 'Enregistrement...' : 'Enregistrer le profil'}</span>
              </button>
            </div>

          </div>

        </div>

      </div>
    </div>
  );
};
