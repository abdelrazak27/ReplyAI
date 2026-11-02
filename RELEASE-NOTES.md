Reply AI 2.2.2

- Correction d’un blocage possible avant le démarrage du délai de sécurité : la limite de 10 secondes couvre maintenant l’attente, le démarrage, l’envoi, la vidange et la lecture de la réponse du moteur.
- Une liaison bloquée est abandonnée. La prochaine demande explicite démarre un moteur neuf ; le texte n’est pas renvoyé automatiquement à Groq.
- Libération de l’état occupé dans le nettoyage du raccourci, y compris en cas d’erreur.
- Journal technique local et borné : étapes et délais uniquement, sans message RP, contexte ni clé API.

Dans Reply AI : Configurations → Mises à jour → Vérifier → Mettre à jour lorsque vous êtes prêt à redémarrer l’application. Profils, clés et historique conservés.
