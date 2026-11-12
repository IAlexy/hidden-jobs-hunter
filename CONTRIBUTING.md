# Guide de Contribution (Contributing)

Merci de votre intérêt pour **HiddenJobs Hunter** ! Ce projet open-source aide les ingénieurs logiciels à détecter les opportunités du « marché invisible » (Hacker News *Who is hiring?*, ATS directs Greenhouse / Lever / Ashby, APIs Remote) et à évaluer leur adéquation technique et administrative depuis la France.

## Prérequis

- **.NET SDK** `>= 8.0`
- **Node.js** `>= 22.0.0` & **npm** `>= 10.0.0`

## Installation et Développement

```bash
# 1. Installer les dépendances Frontend
cd frontend && npm install && cd ..

# 2. Restaurer les packages NuGet Backend & Tests
dotnet restore HiddenJobsHunter.sln

# 3. Lancer le Backend .NET 8 (Port 5000)
dotnet run --project backend/HiddenJobsHunter.Api.csproj --launch-profile http

# 4. Lancer le Frontend React 19 / Vite (Port 5173)
npm --prefix frontend run dev
```

Vous pouvez également utiliser `./start.sh` ou `./launcher.sh` à la racine du dépôt.

## Qualité du Code, Analyse Statique Sonar et Tests

Avant d'ouvrir une Pull Request, assurez-vous que l'ensemble des vérifications de qualité (`SonarAnalyzer.CSharp`, `oxlint`, `tsc`) et des suites de tests passent avec **0 warning et 0 erreur** :

```bash
# Depuis la racine du projet :

# Exécuter l'ensemble des tests (Backend xUnit + Frontend Vitest)
npm test

# Exécuter uniquement les tests Backend (.NET 8 xUnit + WebApplicationFactory)
npm run test:backend

# Exécuter uniquement les tests Frontend (Vitest Unit + UI)
npm run test:frontend

# Vérifier les linters et l'analyse statique (SonarAnalyzer C# + Oxlint TS/React)
npm run lint

# Compiler l'ensemble du projet (Backend + Frontend production bundle)
npm run build
```

## Principes Architecturaux à Respecter

1. **Zéro Dépendance Obligatoire à une Clé API Externe** : Le moteur heuristique local (`AiMatcherService` en mode `Heuristic`) doit toujours être capable de calculer les scores de matching, d'analyser l'éligibilité depuis la France (CDI, EOR, Contractor B2B), de générer des pitchs FR/EN et d'extraire un CV hors-ligne. Les intégrations LLM (`Gemini`, `OpenAI`, `Ollama`) viennent enrichir ce socle avec basculement automatique (*fallback*) sur le moteur heuristique en cas d'erreur réseau.
2. **Sécurité et Anonymisation** : Aucune clé API ni donnée personnelle ne doit être committée. Les clés API stockées dans SQLite sont systématiquement masquées (`********`) par l'API `GET /api/settings`.
3. **Qualité Sonar Stricte** : Tout nouveau code C# ou TypeScript doit compiler sans aucun avertissement (`0 Warning(s)`) avec `SonarAnalyzer.CSharp` et `oxlint`.
