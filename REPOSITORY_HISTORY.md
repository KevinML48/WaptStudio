# Historique du depot WaptStudio

Ce document donne une vue rapide des points d'entree utiles pour relire tout le parcours du projet directement depuis GitHub.

## Branches importantes

- `phase-3-build-and-dryrun`
  - branche de travail principale de la phase 3
  - branche par defaut actuelle du depot
  - contient le detail des evolutions avant integration finale dans `master`

- `master`
  - branche de base historique du projet
  - contient maintenant l'integration complete de la phase 3

## Pull request importante

- PR `#1`
  - fusion de `phase-3-build-and-dryrun` vers `master`
  - utile pour relire la consolidation globale des changements phase 3

## Tags de jalons

### Socle avant phase 3

- `waptstudio-phase2-freeze`
  - gel du socle WaptStudio v1 avant les evolutions phase 3

### Jalons de phase 3

- `waptstudio-phase3-00-dryrun-prep`
- `waptstudio-phase3-01-build-restored`
- `waptstudio-phase3-02-manual-build`
- `waptstudio-phase3-03-sync-plan`
- `waptstudio-phase3-04-signing`
- `waptstudio-phase3-05-interactive-workflows`
- `waptstudio-phase3-06-inventory-readiness`
- `waptstudio-phase3-07-identity-stability`
- `waptstudio-phase3-08-publication-prep`
- `waptstudio-phase3-09-ui-polish`
- `waptstudio-phase3-10-finalization`

## Comment explorer le projet sur GitHub

1. commencer par [PHASES.md](PHASES.md)
2. ouvrir ensuite les tags du depot pour naviguer par jalons
3. consulter la PR `#1` pour la vision agregée de la phase 3
4. comparer `master` et `phase-3-build-and-dryrun` si besoin de relire l'integration finale

## Repere d'integration finale

- integration de la phase 3 dans `master`
  - commit de merge local publie sur `master`
  - message: `merge: integrate phase 3 history into master`

## Convention recommandee pour la suite

- documenter chaque phase significative dans [PHASES.md](PHASES.md)
- poser un tag explicite a chaque jalon important
- conserver une PR de synthese lorsqu'une grande branche de travail revient dans `master`