# Reply AI 2.2.2

- Les appels au moteur ont maintenant un délai global de 10 secondes, y compris l’envoi de la demande. Une liaison bloquée est abandonnée et la demande suivante recrée le moteur.
- La copie, la lecture et la restauration du presse-papiers sont isolées dans un processus STA séparé. Un appel Windows bloqué expire après 2 secondes sans figer l’interface ni les raccourcis.
- Les mêmes protections couvrent le Ctrl+Z et la copie du résultat depuis Essai RP. Les vérifications de sélection, de fenêtre et de propriété du presse-papiers sont conservées.
- Aucune requête n’est automatiquement renvoyée après un blocage. Le texte n’est jamais envoyé dans le jeu automatiquement.
- Journal technique local limité : étapes, résultats et durées, sans texte RP, contexte ou clé API.

Installer depuis Configurations → Mises à jour quand vous êtes prêt à relancer l’application.
