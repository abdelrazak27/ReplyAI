# Traduction locale du chat

Le chat entrant EN → FR utilise Helsinki-NLP/opus-mt-en-fr, révision
`dd7f6540a7a48a7f4db59e5c0b9c42c8eea67f18`, converti en CTranslate2 int8.
Le moteur tourne sur CPU avec deux threads. Il ne contient aucun client réseau
et ne reçoit aucune clé API. Une erreur conserve le message original ; elle ne
déclenche pas de bascule automatique vers Groq.

Dans Chat FR, activer le mode déplacement permet le clic droit sur une ligne :
afficher l’original ou demander une retraduction à Groq. Cette dernière utilise
les connexions API et quotas habituels. Le modèle local peut traduire littéralement
l’argot, perdre une nuance ou se tromper sur un accord ; l’original reste accessible.

Sources et licences :
- https://huggingface.co/Helsinki-NLP/opus-mt-en-fr (Apache-2.0)
- https://github.com/OpenNMT/CTranslate2 (MIT)
- https://github.com/google/sentencepiece (Apache-2.0)

Construction : installer `requirements-local-translation.txt` dans un environnement
Python séparé, puis exécuter `CONSTRUIRE-TRADUCTEUR.ps1 -Python <python.exe> -Destination <dossier>`.
Le téléchargement du modèle se produit à la construction seulement. Copier le
contenu du dossier produit `reply-translate` dans `engine-local`, près de ReplyAI.exe.
Le programme de conversion et PyTorch ne sont pas distribués dans le moteur.

Validation réelle sans API :
`python tests/test_local_translation.py <engine-local/reply-translate.exe>`.
`tests/LocalTranslatorTests.cs` vérifie aussi le protocole depuis C#.
