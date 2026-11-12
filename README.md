# Reply AI

Correction, reformulation et génération de textes RP à partir d'une sélection, sous Windows.

## Installation

Télécharger [la dernière version](https://github.com/abdelrazak27/ReplyAI/releases/latest), extraire le ZIP puis lancer **ReplyAI.exe**. Quitter l'ancienne version lors de la première migration. Les prochaines mises à jour s'installent depuis **Configurations → Mises à jour**.

## Utilisation

Créer un personnage, ajouter ses mots et sa clé API Groq dans **Connexion & API**, puis sélectionner un texte dans l'application cible et utiliser le raccourci configuré. Aucun message n'est envoyé automatiquement dans le jeu.

Les profils et dictionnaires sont importables et exportables. Les clés sont chiffrées pour le compte Windows et exclues des exports. Les textes sélectionnés et le contexte actif sont transmis à Groq lors d'une demande. Chaque utilisateur fournit sa propre clé et respecte les quotas de son organisation.

## Nouveautés 2.3

- **Chat FR** traduit le chat entrant de FiveM sur le PC. Dans le mode déplacement de l’overlay, un clic droit sur une ligne permet d’afficher l’original ou de demander une retraduction avec Groq.
- **FR-EN** produit les messages RP en anglais. En génération **Auto** ou **Entre étoiles**, un geste peut introduire une réplique avec le deux-points avant l’astérisque fermant.
- Les consignes des trois modes préservent les temps de l’original.

## Nouveautés 2.2

- Consommation : session, jour et mois, moyennes par fonction et jetons mesurés.
- État des clés par modèle, organisation et délai de réessai après quota.
- Modèles expliqués, réglage de correction dans Avancé.
- Ctrl+Z : annulation gardée du dernier remplacement, avant retouche du champ et pendant 30 secondes.
- Styles de dialogue Toxico et Crapule.

## Développement

Windows, .NET Framework 4.8 et Python 3.13.

```powershell
./CONSTRUIRE-MOTEUR.ps1
./CONSTRUIRE.ps1
./tests/VERIFIER.ps1
./tests/VERIFIER-MAJ.ps1
./PUBLIER.ps1
```

Le workflow GitHub Actions teste et publie un ZIP Windows lorsque VERSION contient une version encore non publiée. Mettre également les numéros d'assemblage à jour dans src/AssemblyInfo.cs.

Les dépendances conservent leurs licences dans le dossier du moteur. Le code de l'application est consultable ici ; aucune licence de redistribution supplémentaire n'est accordée.
