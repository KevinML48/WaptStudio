# WaptStudio

WaptStudio est un outil Windows local en WinForms, developpe en C# sur .NET 10, pour gerer les paquets CD48 de maniere claire, securisee et exploitable.

## Chronologie du projet

Pour suivre clairement toutes les etapes importantes du projet directement depuis le depot GitHub:

- consultez [PHASES.md](PHASES.md) pour le detail des phases et des jalons
- utilisez les tags Git du depot pour retrouver rapidement les points de passage majeurs

Le produit final est centre sur un seul objectif metier:

- charger un dossier racine de paquets CD48
- inventorier les paquets WAPT detectes
- classer les paquets en MSI, EXE ou AUTRES
- remplacer proprement un installateur MSI/EXE
- synchroniser setup.py, control, version, name, description et description_fr quand le remplacement est fiable
- calculer un verdict readiness lisible
- declencher Build, Sign, publication preparee, Upload direct, Uninstall et Audit depuis l'interface
- conserver des workflows manuels securises pour les actions WAPT interactives
- garder une tracabilite locale via historique, logs, rapports et sauvegardes

## Architecture

La solution conserve l'architecture existante et la clarifie.

- [WaptStudio.App/WaptStudio.App.csproj](WaptStudio.App/WaptStudio.App.csproj): interface WinForms
- [WaptStudio.Core/WaptStudio.Core.csproj](WaptStudio.Core/WaptStudio.Core.csproj): logique metier, services WAPT, historique, sauvegardes
- [WaptStudio.Tests/WaptStudio.Tests.csproj](WaptStudio.Tests/WaptStudio.Tests.csproj): tests unitaires et tests de regression

Services metier principaux:

- [WaptStudio.Core/Services/PackageCatalogService.cs](WaptStudio.Core/Services/PackageCatalogService.cs): scan du catalogue CD48
- [WaptStudio.Core/Services/PackageClassificationService.cs](WaptStudio.Core/Services/PackageClassificationService.cs): classification MSI / EXE / AUTRES
- [WaptStudio.Core/Services/PackageInspectorService.cs](WaptStudio.Core/Services/PackageInspectorService.cs): analyse detaillee d'un paquet
- [WaptStudio.Core/Services/PackageUpdateService.cs](WaptStudio.Core/Services/PackageUpdateService.cs): plan de synchronisation et remplacement d'installateur
- [WaptStudio.Core/Services/PackageValidationService.cs](WaptStudio.Core/Services/PackageValidationService.cs): readiness metier
- [WaptStudio.Core/Services/BackupRestoreService.cs](WaptStudio.Core/Services/BackupRestoreService.cs): sauvegarde et restauration
- [WaptStudio.Core/Services/WaptCommandService.cs](WaptStudio.Core/Services/WaptCommandService.cs): build, sign, upload direct, audit et uninstall via WAPT
- [WaptStudio.Core/Services/HistoryService.cs](WaptStudio.Core/Services/HistoryService.cs): historique SQLite local

## Interface finale

L'ecran principal est organise autour de cinq zones utiles:

- inventaire des paquets CD48
- detail du paquet selectionne
- verdict readiness
- journal d'execution
- historique local

L'interface est refondue en style WinForms moderne sobre:

- cartes et sections separees
- hierarchie visuelle claire
- badges de readiness
- couleurs d'etat vert / orange / rouge / bleu-gris
- boutons d'action plus lisibles
- micro-animation discrete sur la grille pour eviter un rendu trop plat

## Inventaire des paquets CD48

Depuis l'ecran principal, WaptStudio permet de:

- choisir un dossier racine configurable
- scanner en mode recursif complet ou semi-recursif avec profondeur definie
- detecter automatiquement les dossiers de paquets WAPT
- afficher une grille inventaire avec:
  - package id
  - nom visible
  - version
  - categorie MSI / EXE / AUTRES
  - maturite
  - readiness
  - date de derniere modification
  - chemin du dossier
- rechercher par texte
- filtrer par categorie
- trier par colonnes
- selectionner un paquet pour voir son detail

La categorisation metier suit les regles suivantes:

- MSI si `install_msi_if_needed(...)` est detecte ou si l'installateur principal est un `.msi`
- EXE si `install_exe_if_needed(...)` est detecte ou si l'installateur principal est un `.exe`
- AUTRES si aucun signal fiable n'est disponible

## Remplacement d'installateur

Le remplacement MSI/EXE suit un flux robuste:

1. selection du nouveau MSI ou EXE
2. affichage d'une etape explicite de gestion de version WAPT
3. detection du type cible
4. identification de l'ancien installateur principal
5. generation d'un plan de synchronisation
6. affichage d'une previsualisation avant application
7. sauvegarde automatique du paquet
8. application du remplacement
9. suppression de l'ancien installateur principal si le nouveau est applique

Garanties apportees:

- aucune suppression sans sauvegarde prealable
- pas de remplacement partiel silencieux
- le package id reste conserve
- aucun changement silencieux de version WAPT
- la version detectee dans le nom du nouvel installateur reste une suggestion
- la version cible est choisie explicitement avant la previsualisation

## Gestion explicite de version WAPT

Avant de lancer la previsualisation, WaptStudio affiche maintenant une etape dediee a la version du paquet. Cette etape presente:

- la version actuelle du paquet
- la version suggeree si elle peut etre deduite de facon fiable depuis le nom du nouvel installateur
- le nouvel installateur selectionne
- trois strategies simples

Strategies disponibles:

1. conserver la version actuelle
2. incrementer uniquement la revision du paquet
3. definir explicitement une nouvelle version

Regles appliquees:

- conserver: la version WAPT reste strictement identique
- incrementer la revision: seule la revision du paquet evolue, par exemple `11.0.0-1` devient `11.0.0-2`
- definir une nouvelle version: la version saisie est validee strictement avant application
- si une version explicite est saisie sans revision alors que le paquet courant utilise deja une revision, WaptStudio normalise de facon coherente en demarrant a `-1`
- le champ `control/package` n'est jamais modifie automatiquement

La version produit et la revision du paquet ont des roles differents:

- la version produit represente la version logicielle transportee par l'installateur
- la revision du paquet represente l'iteration du paquet WAPT autour de cette version produit

Exemples:

- conserver `9.1.1-1` permet de remplacer un binaire sans changer la version WAPT
- incrementer la revision transforme `9.1.1-1` en `9.1.1-2`
- remplacer explicitement la version permet un cas comme `9.1.1-1 -> 11.0.0-1`

## Synchronisation complete du paquet

Apres remplacement, WaptStudio synchronise les elements juges fiables:

- `setup.py`
- `WAPT/control` ou `control`
- version
- name
- description
- description_fr
- nom attendu du `.wapt`
- nom du dossier racine quand le renommage est clairement derivable de l'ancienne version

Le moteur limite volontairement les remplacements pour rester sur des motifs connus et securises:

- `install_msi_if_needed(...)`
- `install_exe_if_needed(...)`
- `print(...)`
- references d'installateur reconnues
- champs standards du control

Les remplacements arbitraires dangereux ne sont pas faits.

## Previsualisation avant application

La fenetre de previsualisation affiche:

- ancien installateur -> nouveau installateur
- ancien type -> nouveau type
- ancienne version -> nouvelle version
- ancien name -> nouveau name
- ancienne description -> nouvelle description
- ancien dossier -> nouveau dossier attendu
- nom attendu du `.wapt`
- fichiers supprimes
- fichiers modifies
- avertissements
- information de sauvegarde

L'utilisateur peut confirmer ou annuler avant toute modification.

## Readiness metier

WaptStudio calcule un verdict unique parmi:

- `PRET POUR BUILD / UPLOAD`
- `PRET AVEC AVERTISSEMENTS`
- `BLOQUE`

Le detail readiness explique pourquoi le paquet est dans cet etat.

Verifications principales:

- presence de `setup.py`
- presence du fichier `control`
- presence de l'installateur principal
- coherence entre installateur detecte et installateur reference
- coherence version / name / descriptions
- accessibilite du dossier paquet
- possibilite d'ecriture
- possibilite de sauvegarde
- disponibilite WAPT
- build possible
- upload possible selon la configuration
- audit possible
- uninstall possible
- presence d'installateurs residuels supplementaires

Le readiness complet peut inclure ou non la verification WAPT detaillee selon le contexte:

- scan inventaire: readiness rapide sans lancer la validation WAPT lourde
- validation utilisateur: readiness complet avec validation WAPT si souhaitee

## Actions disponibles dans l'interface

Actions metier exposees:

- `Analyser`
- `Remplacer MSI/EXE`
- `Valider readiness`
- `Build`
- `Sign`
- `Publier...`
- `Construire puis publier...`
- `Audit`
- `Uninstall`
- `Restaurer derniere sauvegarde`
- `Workflow manuel`
- `Exporter rapport`

Messages utilisateur utilises:

- succes reel
- succes simule
- action manuelle requise
- action bloquee
- erreur reelle

Le produit n'affiche pas de faux succes.

## Portabilite poste par poste

WaptStudio est prepare pour etre distribue a plusieurs utilisateurs Windows sans dependre du profil du poste de developpement.

Principes appliques:

- aucune donnee de travail n'est stockee dans le depot source
- la configuration utilisateur est locale a la session Windows
- les logs, l'historique, le cache et les sauvegardes sont separes
- les secrets ne sont pas ecrits en clair dans `appsettings.json`
- l'executable WAPT est detecte automatiquement quand c'est possible

Emplacements locaux par defaut:

- `%LOCALAPPDATA%\\WaptStudio\\config`
- `%LOCALAPPDATA%\\WaptStudio\\data`
- `%LOCALAPPDATA%\\WaptStudio\\cache`
- `%LOCALAPPDATA%\\WaptStudio\\logs`
- `%LOCALAPPDATA%\\WaptStudio\\backups`

Option d'override locale:

- definir la variable d'environnement `WAPTSTUDIO_HOME` pour deplacer tout le stockage local WaptStudio vers un autre dossier

Ce que WaptStudio detecte automatiquement:

- `wapt-get.exe` si l'executable est deja resolvable via le `PATH`
- `wapt-get.exe` via `WAPT_HOME` ou `WAPT_ROOT`
- les emplacements Windows standards comme `Program Files\\wapt`

Ce qui reste a configurer par utilisateur:

- le certificat de signature personnel
- l'URL du depot d'upload
- le dossier catalogue a inventorier
- tout chemin WAPT specifique si l'auto-detection ne suffit pas

Au premier lancement sur un nouveau poste, WaptStudio affiche un diagnostic d'environnement. Ce rapport indique:

- les dossiers locaux utilises
- le chemin WAPT detecte ou manquant
- l'etat du certificat de signature
- les actions manuelles encore necessaires

## Build, publication finale, Audit et Uninstall via WAPT

Toutes les commandes WAPT passent par [WaptStudio.Core/Services/WaptCommandService.cs](WaptStudio.Core/Services/WaptCommandService.cs).

Commandes prises en charge:

- verification WAPT
- validation de paquet
- build
- sign
- upload direct
- audit
- uninstall

Placeholders disponibles dans les templates:

- `{packageFolder}`
- `{waptFilePath}`
- `{packageId}`
- `{signingKeyPath}`
- `{uploadRepositoryUrl}`
- `{repositoryOption}`

## Publication Windows interne

Le script [Build-Release.ps1](Build-Release.ps1) produit maintenant une version Windows `win-x64` self-contained, directement distribuable sur un autre poste sans SDK .NET.

Commande recommandee:

```powershell
.\Build-Release.ps1
```

Resultat attendu:

- compilation `Release`
- tests de la solution
- publication `win-x64` self-contained
- sortie dans `dist\win-x64\self-contained`

Le script [Start-WaptStudio.ps1](Start-WaptStudio.ps1) sait maintenant:

- lancer l'executable publie s'il existe a cote du script dans `win-x64\self-contained`
- sinon revenir au mode developpement `dotnet run`

Pour une distribution interne simple:

1. lancer `Build-Release.ps1`
2. copier le dossier `dist`
3. sur le poste cible, lancer `Start-WaptStudio.ps1` depuis ce dossier ou directement `WaptStudio.App.exe`
4. verifier le diagnostic du premier lancement puis completer les parametres locaux si necessaire

## Paquet WAPT de WaptStudio

WaptStudio peut maintenant etre lui-meme deploye sous forme de paquet WAPT, en partant exclusivement du resultat publie et non du code source brut.

Strategie retenue:

- publish `Release` self-contained `win-x64`
- staging d'un paquet WAPT dans `artifacts\wapt-package\cd48-waptstudio`
- installation applicative dans `Program Files\WaptStudio`
- raccourci Menu Demarrer commun
- raccourci Bureau desactive par defaut mais configurable dans le `setup.py`
- aucune donnee utilisateur installee dans `Program Files`
- aucune suppression des donnees utilisateur dans `%LOCALAPPDATA%\WaptStudio` lors d'une mise a jour ou d'une desinstallation

Fichiers de packaging ajoutes:

- [Build-WaptStudio-Package.ps1](Build-WaptStudio-Package.ps1)
- [Test-WaptStudio-Package.ps1](Test-WaptStudio-Package.ps1)
- [packaging/wapt/README.md](packaging/wapt/README.md)
- [packaging/wapt/cd48-waptstudio/setup.py](packaging/wapt/cd48-waptstudio/setup.py)
- [packaging/wapt/cd48-waptstudio/WAPT/control](packaging/wapt/cd48-waptstudio/WAPT/control)

Commandes recommandees:

```powershell
.\Build-WaptStudio-Package.ps1
.\Test-WaptStudio-Package.ps1
```

Pour construire directement le `.wapt` si `wapt-get.exe` est disponible:

```powershell
.\Build-WaptStudio-Package.ps1 -BuildWithWapt
.\Test-WaptStudio-Package.ps1 -BuildWithWapt
```

Ce que le paquet installe globalement:

- `WaptStudio.App.exe`
- les DLL et dependances du publish self-contained
- le raccourci Menu Demarrer commun

Ce qui reste par utilisateur:

- `appsettings.json`
- historique SQLite
- logs
- cache
- sauvegardes
- configuration WAPT locale
- certificat de signature local si l'utilisateur en configure un

Le template du paquet est versionne dans le depot, tandis que le paquet complet genere reste dans `artifacts`, afin de ne pas versionner les binaires publies.
- `{overwriteFlag}`

Templates par defaut utiles:

- test WAPT: `--version`
- validation: `show {packageFolder}`
- build: `build-package {packageFolder}`
- sign: `sign-package {packageFolder}`
- upload direct: `upload-package {waptFilePath}`

## Flux recommande de publication

Le flux recommande dans WaptStudio est desormais explicite:

1. construire le vrai `.wapt` depuis WaptStudio
2. verifier le readiness du paquet
3. ouvrir la synthese de publication finale
4. publier via WAPT Console si l'upload direct serveur n'est pas disponible ou pas maitrise

WaptStudio distingue deux modes:

- `Upload direct`: conserve pour les environnements qui disposent reellement d'un acces serveur operationnel et des identifiants admin adequats
- `Preparation pour WAPT Console`: mode recommande par defaut quand l'equipe ne maitrise pas l'authentification serveur

La synthese finale affiche:

- paquet pret ou non
- chemin exact du vrai `.wapt`
- package id
- version
- maturite
- action recommandee

Quand le paquet est pret pour WAPT Console, WaptStudio permet de:

- copier le chemin du `.wapt`
- ouvrir le dossier contenant le `.wapt`
- ouvrir le dossier source du paquet
- marquer la publication comme a faire dans WAPT Console
- audit: `audit {packageId}`
- uninstall: `remove {packageId}`

## Workflows interactifs securises

Pour les actions interactives, WaptStudio conserve les workflows deja introduits:

- prompt temporaire de mot de passe certificat pour `Build` et `Sign`
- prompt temporaire de login/mot de passe admin uniquement pour `Upload direct`
- publication via WAPT Console recommandee par defaut si l'environnement serveur n'est pas maitrise

Garanties securite:

- aucun secret n'est persiste dans `AppSettings`
- aucun secret n'est stocke dans SQLite
- aucun secret n'est ecrit dans les logs ou le rapport exporte
- les sorties sont nettoyees via redaction
- les secrets sont gardes uniquement en memoire pendant l'action puis effaces

Si `wapt-get` ne supporte pas une automatisation non interactive fiable:

- WaptStudio prepare un workflow manuel assiste
- la commande est affichee sans secret
- l'utilisateur peut ouvrir un PowerShell dans le dossier du paquet
- le resultat manuel est rattache a l'historique

## Sauvegardes et restauration

Avant un remplacement d'installateur, WaptStudio cree une sauvegarde du paquet.

Le systeme fournit:

- sauvegarde automatique avant modification
- stockage structure par paquet
- manifeste JSON de sauvegarde
- bouton pour ouvrir le dossier des sauvegardes
- restauration de la derniere sauvegarde du paquet selectionne
- resume des fichiers restaures

## Historique et rapports

L'historique SQLite enregistre notamment:

- action
- paquet
- date
- statut
- duree
- message
- chemin du paquet
- chemin du `.wapt` si pertinent
- verdict readiness si pertinent
- version avant / apres

Le rapport exporte depuis l'interface contient:

- etat du paquet selectionne
- verdict readiness
- logs recents
- actions recentes
- informations utiles pour decider du build ou de l'upload

## Configuration locale

Au premier lancement, l'application cree un espace local sous `%LOCALAPPDATA%\WaptStudio`.

Par defaut:

- configuration JSON: `%LOCALAPPDATA%\WaptStudio\config\appsettings.json`
- historique SQLite: `%LOCALAPPDATA%\WaptStudio\data\history.db`
- logs: `%LOCALAPPDATA%\WaptStudio\logs`
- backups: `%LOCALAPPDATA%\WaptStudio\backups`

La configuration permet notamment de definir:

- racine catalogue CD48
- mode de scan recursif ou semi-recursif
- chemin de `wapt-get.exe`
- timeout global
- dry-run
- chemins logs et backups
- templates build / sign / upload / audit / uninstall
- cle de signature

## Tests couverts

La suite de tests couvre les points critiques suivants:

- classification MSI / EXE / AUTRES
- scan de catalogue CD48
- remplacement d'installateur
- suppression de l'ancien installateur
- synchronisation control
- synchronisation setup.py
- renommage du dossier paquet
- readiness metier
- plan de synchronisation et previsualisation
- upload sans repository obligatoire si non requis
- workflows manuels et assistes Build / Sign / Upload
- non-persistance des secrets
- sauvegarde et restauration

Validation actuelle:

- build Release: OK
- tests Release: 25/25 OK

## Bonnes pratiques d'usage

Approche recommandee:

1. configurer la racine CD48 et les templates WAPT reels
2. commencer avec le `dry-run` active
3. scanner le catalogue puis filtrer les paquets cibles
4. verifier le readiness avant tout remplacement
5. controler la previsualisation avant application
6. verifier la sauvegarde creee avant un remplacement reel
7. ne passer en execution reelle qu'une fois le readiness compris

## Limites connues

- la fiabilite exacte de `Build`, `Sign`, `Upload`, `Audit` et `Uninstall` depend de votre version reelle de WAPT et de ses arguments locaux
- le renommage du dossier paquet n'est applique que quand il peut etre deduit de maniere suffisamment fiable
- la synchronisation de textes reste volontairement prudente et ne tente pas de remplacements generiques non maitrises
- `Audit` et `Uninstall` reposent sur la commande configuree et sur le `packageId` detecte; il faut confirmer les templates adaptes a votre environnement
- l'UI reste WinForms: le rendu est modernise, mais sans moteur graphique externe ni theming avance

## Commandes utiles

Depuis PowerShell a la racine du depot:

```powershell
dotnet build .\WaptStudio.sln
dotnet test .\WaptStudio.sln
dotnet run --project .\WaptStudio.App\WaptStudio.App.csproj
```
