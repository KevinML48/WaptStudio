# WaptStudio

WaptStudio est une application Windows locale en WinForms, developpee en C# sur .NET 10, pour analyser, modifier, valider, construire, signer et uploader des paquets WAPT sans interface web.

La phase 2 de consolidation fournie dans ce depot rend la V1 plus exploitable pour un premier lancement reel sur une machine Windows avec WAPT installe localement.

## Contenu reel de la solution

- [WaptStudio.sln](WaptStudio.sln)
- [WaptStudio.App/WaptStudio.App.csproj](WaptStudio.App/WaptStudio.App.csproj)
- [WaptStudio.Core/WaptStudio.Core.csproj](WaptStudio.Core/WaptStudio.Core.csproj)
- [WaptStudio.Tests/WaptStudio.Tests.csproj](WaptStudio.Tests/WaptStudio.Tests.csproj)
- [Start-WaptStudio.ps1](Start-WaptStudio.ps1)
- [Build-Release.ps1](Build-Release.ps1)

## Architecture actuelle

### Application WinForms

- [WaptStudio.App/Program.cs](WaptStudio.App/Program.cs): point d'entree, gestion globale des exceptions
- [WaptStudio.App/Bootstrap/AppRuntime.cs](WaptStudio.App/Bootstrap/AppRuntime.cs): composition des services
- [WaptStudio.App/Forms/MainForm.cs](WaptStudio.App/Forms/MainForm.cs): ecran principal
- [WaptStudio.App/Forms/SettingsForm.cs](WaptStudio.App/Forms/SettingsForm.cs): configuration locale
- [WaptStudio.App/Forms/HistoryDetailsForm.cs](WaptStudio.App/Forms/HistoryDetailsForm.cs): consultation detaillee d'une entree d'historique

### Couche metier

- [WaptStudio.Core/Configuration/AppPaths.cs](WaptStudio.Core/Configuration/AppPaths.cs): chemins locaux
- [WaptStudio.Core/Models/AppSettings.cs](WaptStudio.Core/Models/AppSettings.cs): configuration persistante
- [WaptStudio.Core/Models/PackageInfo.cs](WaptStudio.Core/Models/PackageInfo.cs): informations de paquet detectees
- [WaptStudio.Core/Models/ValidationResult.cs](WaptStudio.Core/Models/ValidationResult.cs): resultat de validation metier
- [WaptStudio.Core/Models/CommandExecutionResult.cs](WaptStudio.Core/Models/CommandExecutionResult.cs): resultat complet d'execution systeme
- [WaptStudio.Core/Models/HistoryEntry.cs](WaptStudio.Core/Models/HistoryEntry.cs): entree d'historique enrichie
- [WaptStudio.Core/Services/SettingsService.cs](WaptStudio.Core/Services/SettingsService.cs): persistance JSON
- [WaptStudio.Core/Services/LogService.cs](WaptStudio.Core/Services/LogService.cs): logs applicatifs
- [WaptStudio.Core/Services/HistoryService.cs](WaptStudio.Core/Services/HistoryService.cs): SQLite locale
- [WaptStudio.Core/Services/CommandExecutionService.cs](WaptStudio.Core/Services/CommandExecutionService.cs): execution systeme avec timeout et capture stdout/stderr
- [WaptStudio.Core/Services/PackageInspectorService.cs](WaptStudio.Core/Services/PackageInspectorService.cs): analyse d'un dossier paquet
- [WaptStudio.Core/Services/PackageUpdateService.cs](WaptStudio.Core/Services/PackageUpdateService.cs): remplacement MSI/EXE et mise a jour de references
- [WaptStudio.Core/Services/PackageValidationService.cs](WaptStudio.Core/Services/PackageValidationService.cs): validation metier consolidee
- [WaptStudio.Core/Services/WaptCommandService.cs](WaptStudio.Core/Services/WaptCommandService.cs): commandes WAPT construites depuis la configuration

### Tests unitaires

- [WaptStudio.Tests/PackageInspectorServiceTests.cs](WaptStudio.Tests/PackageInspectorServiceTests.cs)
- [WaptStudio.Tests/PackageUpdateServiceTests.cs](WaptStudio.Tests/PackageUpdateServiceTests.cs)
- [WaptStudio.Tests/PackageValidationServiceTests.cs](WaptStudio.Tests/PackageValidationServiceTests.cs)

## Fonctionnalites actuellement implementees

### MainForm

L'ecran principal permet de:

- selectionner un dossier de paquet
- afficher le paquet courant, la version detectee, l'installeur principal, les chemins importants et les installateurs trouves
- afficher le statut WAPT disponible, indisponible ou dry-run
- afficher le dernier resultat d'action
- analyser le paquet
- remplacer un MSI ou EXE
- lancer une validation metier + validation WAPT
- lancer build, sign et upload via `WaptCommandService`
- tester WAPT
- ouvrir le dossier paquet
- ouvrir le dossier de logs
- sauvegarder un rapport texte
- consulter l'historique et ouvrir le detail d'une entree

### SettingsForm

La configuration locale permet de definir:

- chemin vers `wapt-get.exe`
- timeout global
- arguments de verification WAPT
- arguments de validation
- arguments de build
- arguments de sign
- arguments d'upload
- mode dry-run
- activation des backups
- activation signature/upload
- option overwrite pour upload
- dossier de logs
- dossier de backups
- chemin de cle de signature
- cible d'upload
- dossier paquet par defaut

## Configuration locale et donnees

Au premier lancement, l'application cree un espace local sous `%LOCALAPPDATA%\WaptStudio`.

Par defaut:

- configuration: `%LOCALAPPDATA%\WaptStudio\config\appsettings.json`
- base SQLite: `%LOCALAPPDATA%\WaptStudio\data\history.db`
- logs: `%LOCALAPPDATA%\WaptStudio\logs`
- backups: `%LOCALAPPDATA%\WaptStudio\backups`

Ces dossiers logs et backups peuvent ensuite etre rediriges dans l'interface de configuration.

## Historique et logs

Chaque action significative peut enregistrer:

- date
- action
- paquet
- statut
- duree
- commande executee
- stdout
- stderr
- utilisateur Windows
- version avant/apres si disponible

L'historique detaille est consultable depuis l'interface.

## Validation metier actuelle

Le service de validation verifie au minimum:

- dossier accessible
- presence de `setup.py`
- presence de `control`
- presence d'au moins un MSI/EXE
- coherence entre installeur reference et fichiers detectes si une reference est trouvee
- detection du nom et de la version
- configuration/disponibilite WAPT
- accessibilite du dossier de backup
- possibilite d'ecriture dans le dossier paquet
- tentative de validation WAPT via la configuration courante

Les severites renvoyees sont:

- `OK`
- `WARNING`
- `ERROR`

## Commandes WAPT configurables

Toutes les commandes WAPT passent par [WaptStudio.Core/Services/WaptCommandService.cs](WaptStudio.Core/Services/WaptCommandService.cs).

Methodes principales:

- `CheckWaptAvailabilityAsync()`
- `ValidatePackageWithWaptAsync(...)`
- `BuildPackageAsync(...)`
- `SignPackageAsync(...)`
- `UploadPackageAsync(...)`

Les arguments proviennent exclusivement de la configuration locale. L'UI ne construit pas elle-meme de commande WAPT.

Placeholders supportes dans les templates d'arguments:

- `{packageFolder}`
- `{signingKeyPath}`
- `{uploadRepositoryUrl}`
- `{repositoryOption}`
- `{overwriteFlag}`

## Dry-run

Quand le mode dry-run est active:

- WaptStudio construit la commande finale
- la commande est affichee dans les logs et historisee
- aucune execution reelle n'est lancee
- le resultat remonte comme une simulation reussie

Ce mode est recommande pour verifier les templates de commande avant tout test reel.

## Prerequis

1. Windows
2. SDK .NET 10 installe et accessible via `dotnet`
3. Outils WAPT installes localement si vous voulez tester les commandes reelles
4. Droits d'ecriture sur le dossier du paquet et sur les repertoires locaux WaptStudio

## Lancement en developpement

Depuis PowerShell a la racine du depot:

```powershell
.\Start-WaptStudio.ps1
```

Ce script:

1. verifie la presence de `dotnet`
2. affiche la version du SDK detecte
3. lance `dotnet run` sur le projet WinForms

## Build Release

Depuis PowerShell a la racine du depot:

```powershell
.\Build-Release.ps1
```

Ce script:

1. verifie la presence de `dotnet`
2. restaure la solution
3. compile en `Release`
4. execute les tests
5. publie l'application dans `dist\publish`
6. copie `README.md`, `Start-WaptStudio.ps1` et `Build-Release.ps1` dans `dist\`

## Premier parametrage WAPT

Dans WaptStudio:

1. ouvrir `Parametres`
2. renseigner le chemin reel de `wapt-get.exe`
3. verifier les templates d'arguments WAPT
4. activer `dry-run`
5. choisir un dossier de logs si necessaire
6. choisir un dossier de backups si necessaire
7. enregistrer

Exemple prudent de templates initiaux:

- test WAPT: `--version`
- validation: `show {packageFolder}`
- build: `build-package {packageFolder}`
- sign: `sign-package --private-key {signingKeyPath} {packageFolder}`
- upload: `upload-package {repositoryOption} {overwriteFlag} {packageFolder}`

Ces templates doivent etre confirmes sur votre version reelle de WAPT.

## Premier test reel avec un paquet WAPT

1. lancer WaptStudio
2. ouvrir `Parametres`
3. renseigner le chemin WAPT reel
4. laisser `dry-run` active pour commencer
5. cliquer sur `Tester WAPT`
6. selectionner un vrai dossier de paquet WAPT
7. cliquer sur `Analyser`
8. verifier les chemins, la version, les installateurs detectes et l'installeur reference
9. cliquer sur `Valider`
10. consulter les `OK`, `WARNING` et `ERROR`
11. si tout est coherent, tester `Construire`, `Signer` ou `Uploader` en dry-run
12. verifier les commandes construites dans les logs et l'historique
13. desactiver ensuite le dry-run pour un test reel controle

## Lecture des logs

- logs applicatifs: dossier configure dans les parametres, sinon `%LOCALAPPDATA%\WaptStudio\logs`
- log en direct: visible dans l'ecran principal
- historique technique: visible dans la grille, avec detail consultable
- rapport: exportable via le bouton `Sauvegarder rapport`

## Cohabitation avec les vraies commandes WAPT

Approche recommande pour limiter les risques:

1. commencer par `Tester WAPT`
2. garder `dry-run` active tant que les templates ne sont pas valides
3. verifier les backups
4. tester sur un paquet de preproduction
5. n'activer `Uploader` que lorsque le repository cible est confirme

## TODO restants clairement assumes

- confirmer la syntaxe exacte des commandes WAPT sur la version reelle de votre poste
- confirmer les options de signature attendues par votre outillage WAPT
- confirmer les options d'upload et l'eventuelle authentification associee
- verifier le comportement exact de `show` ou de la commande de validation la plus adaptee a votre distribution WAPT

## Notes de verification

- la consolidation a ete faite en conservant l'architecture existante
- les forms n'executent pas de commande WAPT en direct
- les scripts et la documentation ci-dessus correspondent au code consolide actuel
- dans l'environnement de generation initial, `dotnet` n'etait pas disponible, donc la verification de compilation reelle doit etre faite sur une machine Windows equipee du SDK .NET 10