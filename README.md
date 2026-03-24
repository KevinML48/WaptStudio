# WaptStudio

WaptStudio est un outil Windows local en WinForms, developpe en C# sur .NET 10, pour gerer les paquets CD48 de maniere claire, securisee et exploitable.

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
2. detection du type cible
3. identification de l'ancien installateur principal
4. generation d'un plan de synchronisation
5. affichage d'une previsualisation avant application
6. sauvegarde automatique du paquet
7. application du remplacement
8. suppression de l'ancien installateur principal si le nouveau est applique

Garanties apportees:

- aucune suppression sans sauvegarde prealable
- pas de remplacement partiel silencieux
- le package id reste conserve
- la version cible est deduite du nouveau fichier si possible
- sinon la version existante est explicitement conservee

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
