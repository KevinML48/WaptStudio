# Paquet WAPT WaptStudio

Ce dossier est un template versionne. Les binaires publies de WaptStudio y sont injectes automatiquement par `Build-WaptStudio-Package.ps1`.

Parametres resolus lors de la generation:

- package id: `__PACKAGE_ID__`
- version: `__PACKAGE_VERSION__`
- runtime source: `__PUBLISH_RUNTIME__`
- publish source: `__PUBLISH_SOURCE__`

Strategie d'installation:

- copie des binaires dans `Program Files\WaptStudio`
- creation d'un raccourci Menu Demarrer commun
- raccourci Bureau desactive par defaut mais configurable dans `setup.py`
- aucune suppression des donnees utilisateur locales dans `%LOCALAPPDATA%\WaptStudio`

Apres generation, le paquet complet est stage dans `artifacts\wapt-package\cd48-waptstudio`.