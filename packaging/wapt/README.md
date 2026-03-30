# Packaging WAPT de WaptStudio

Ce dossier contient le template versionne du paquet WAPT de WaptStudio.

Principe retenu:

- le depot versionne uniquement le template du paquet
- les binaires publies ne sont pas commits
- le staging du vrai paquet est genere dans `artifacts\wapt-package\cd48-waptstudio`
- la source du paquet est le publish `win-x64` self-contained de WaptStudio

Commandes recommandees depuis la racine du depot:

```powershell
.\Build-WaptStudio-Package.ps1
.\Test-WaptStudio-Package.ps1
```

Pour construire directement le `.wapt` si `wapt-get.exe` est disponible:

```powershell
.\Build-WaptStudio-Package.ps1 -BuildWithWapt
```