# Reply AI — version 2.2.4

Correcteur de sélection pour Windows et FiveM, utilisant exclusivement Groq. Python et le SDK Groq sont inclus. Aucun autre fournisseur n’est proposé ou appelé.

## Nouvelle identité et cadre (2.0)

Reply AI conserve les fonctions de Parole RP. Fermez l’ancienne version avant de lancer la nouvelle : les deux utilisent le même verrou pour éviter deux moteurs de raccourcis simultanés.

La base graphique utilise des tons graphite et blanc cassé, des angles droits, des traits fins et une typographie Segoe UI. Le logo vectoriel est un signe de réponse. Les sources SVG, la version PNG et les fichiers de direction artistique sont dans `brand`. L’icône Windows comporte sept tailles de 16 à 256 pixels.

La navigation est regroupée sous **Général** et **Configurations**, avec les pages visuellement imbriquées et un repère de sélection. Le header contient seulement le personnage actif et l’état passif. Le footer conserve ses messages et présente séparément Correction, Reformulation et Génération ; cliquez sur l’un des trois pour ouvrir ses réglages.

Près de l’horloge, un **clic gauche simple** restaure la fenêtre. Le clic droit conserve le menu pour ouvrir Reply AI, choisir le personnage ou quitter. La refonte du contenu des pages reste une étape séparée.

Vos données et clés existantes sont retrouvées dans le dossier historique `%LOCALAPPDATA%\ParoleRP` : ne le renommez pas. Les identifiants internes et le format JSON historique restent compatibles. Les nouveaux exports sont nommés `.replyai.json` et les anciens `.parolerp.json` restent importables. Les clés ne sont pas exportées.

## Démarrer

Fermez l’ancienne version puis lancez **ReplyAI.exe** ou **LANCER.cmd**. Gardez le dossier **engine-v1.9** à côté de l’exécutable. Windows 64 bits et .NET Framework 4.8 sont nécessaires ; aucune installation Python n’est requise.

L’application s’ouvre directement sur Personnages, sans tutoriel ni parcours imposé. Au premier lancement, l’espace est vide : créez un personnage ou importez un profil. Tous les onglets sont accessibles immédiatement ; ajoutez votre clé depuis Connexion & API. Aucun profil ni dictionnaire n’est préparé automatiquement.

Les profils, dictionnaires, raccourcis et clés Groq des anciennes versions sont conservés. Au premier lancement de cette version, les connexions de l’ancien fournisseur sont retirées du coffre local et son ancien fichier de clé est supprimé. Les anciens exports de personnages restent importables ; leur configuration de connexion est adaptée à Groq. Cette suppression locale ne révoque pas une clé chez son fournisseur.

## Navigation simplifiée (1.6.5)

L’application s’ouvre directement sur **Personnages**. La barre latérale commence directement par les onglets : logo, slogans, encart décoratif et numéro de version sont retirés. L’ancienne Vue d’ensemble est supprimée. L’encart explicatif « Votre style reste le vôtre » est retiré de l’onglet Essai RP. Les cinq onglets restants gardent leurs fonctions : Personnages, Dictionnaires, Essai RP, Connexion & API, Import & export.

Le sélecteur du personnage actif et l’état du correcteur sont visibles dans le header de chaque onglet. L’état est purement informatif : il ne reçoit ni clic ni focus clavier. Il indique configuration, clé manquante, raccourci inactif, correction en cours ou prêt. La fonction de pause est supprimée, y compris dans le menu de notification : chaque correction se déclenche uniquement à votre demande avec la sélection et le raccourci. L’état prêt repose sur la clé enregistrée et le raccourci actif ; une connexion réseau est réellement vérifiée lors d’un essai ou d’une correction.

Le footer affiche le raccourci choisi : cliquez dessus pour ouvrir ses réglages. Les compteurs et leur menu sont supprimés. Les messages de résultat ou d’erreur restent dans le footer, tronqués si nécessaire avec le texte complet au survol.

Le tutoriel et sa barre de progression sont supprimés. Une configuration commencée dans une ancienne version est conservée, sans reprendre ce parcours.

## Clés Groq : liste unique (1.7)

**Connexion & API** réunit la clé principale et toutes les clés de secours. Seuls leurs **six derniers caractères** apparaissent dans la liste. Les clés complètes restent chiffrées sur le PC et ne sont pas affichées ni préremplies dans les champs de modification.

Cliquez sur **+ Ajouter une clé**. Si aucune principale n’existe, elle est configurée en premier. Sinon, la nouvelle clé devient un secours. Donnez un nom, collez la clé et indiquez le compte Groq qui partage le quota. Utilisez le même nom de compte pour les clés d’une même organisation. Le modèle est accessible dans un volet replié de cette petite fenêtre.

Chaque ligne possède **Tester** et un menu **⋯** pour modifier ou supprimer la clé ; les secours peuvent aussi monter ou descendre dans la liste. Une modification sans nouvelle clé conserve la clé enregistrée. Une case par secours permet de l’exclure des tentatives. Le bouton global **Secours automatiques si quota atteint** active le basculement.

Les liens **Créer une clé** et **Quotas** ouvrent la console Groq. Les modèles pris en charge restent Qwen 3.8 27B (proposé), GPT-OSS 120B et GPT-OSS 20B. Qwen utilise le mode sans raisonnement prolongé ; GPT-OSS utilise le réglage minimal pris en charge.

Pour définir un raccourci, cliquez sur son champ ou son libellé puis appuyez sur la combinaison souhaitée. Il est enregistré au relâchement de la touche ; Échap annule. Les deux fonctions ne peuvent pas utiliser la même combinaison. Les lettres, chiffres, touches de fonction, touches de navigation et du pavé numérique peuvent être combinés librement avec Ctrl, Alt, Maj et Win. Windows peut réserver ou refuser certaines combinaisons. S’il est occupé ou refusé, le précédent est restauré. L’option de réduction près de l’horloge est aussi enregistrée immédiatement. Le diagnostic du raccourci reste disponible, replié par défaut.

Aucune clé existante n’est remplacée par cette refonte. Le chiffrement Windows, l’exclusion des exports, les limites et les tarifs de vos comptes restent les mêmes. L’interface n’active aucun abonnement ni moyen de paiement. La sélection et son contexte vont uniquement à Groq.

## Reformulation (1.8.2)

La correction garde votre voix actuelle. La **reformulation** reconstruit librement les phrases à partir de l’idée de la sélection dans les styles du personnage actif, avec son propre raccourci : **Alt + Maj + Espace** par défaut. Il se règle par capture dans **Connexion & API**, indépendamment de la correction. Un clic droit sur son champ permet de le désactiver. En cas de conflit Windows, choisissez un autre raccourci.

Dans **Personnages**, ouvrez **Styles d’écriture**. Choisissez séparément le style du **Dialogue** (naturel, soutenu, familier, street discret, street US discret, populaire, ancienne génération…) et des **Actions** (simple, concis, descriptif, détaillé, littéraire, cinématographique). Des précisions facultatives permettent de personnaliser chacun. Enregistrez le profil ; les accords restent réglés par le genre grammatical du personnage, indépendamment du style.

Les passages entre étoiles utilisent le style des actions, même au milieu d’un dialogue. Les lignes /me et /ame sont des actions ; /do décrit un fait ou l’environnement. La reformulation conserve la commande, les faits et les sujets : elle n’est pas une génération de nouveaux événements. Les commandes, étoiles, bégaiements et interruptions RP restent protégés avant collage. En reformulation, les entrées du dictionnaire contenant une majuscule restent contrôlées comme noms propres ; le vocabulaire courant peut changer pour correspondre au style. Les consignes demandent aussi de préserver les noms et appellations culturelles. Les limites de longueur relative et de paragraphes de la correction sont retirées pour la reformulation : elle peut changer de mots et de construction, ou développer une idée courte. La correction ordinaire conserve ses protections précédentes.

Pour essayer sans remplacer une sélection, choisissez **Reformulation** dans **Essai RP**, puis **Appliquer**. Le raccourci utilise le même circuit de sélection, revalidation et remplacement que la correction et n’envoie jamais le message. La réponse reste dans l’application si une protection bloque son remplacement.

La reformulation utilise **GPT-OSS 120B sur Groq**, indépendamment du modèle de correction choisi par clé. Le modèle réellement utilisé apparaît dans le résultat de l’essai ; le volet de modèle d’une clé concerne maintenant explicitement la correction. Aucun abonnement ni paiement n’est activé par l’application. La reformulation effectue un seul appel Groq avec le contexte complet et les styles du profil ; ses consignes peuvent consommer davantage de jetons qu’une correction. Les corrections ordinaires gardent leur préparation précédente. Le délai maximal reste dix secondes pour toute la chaîne, et seuls les dépassements de quota déclenchent un secours. Les nouveaux styles et raccourcis sont sauvegardés et exportés avec les données, sans les clés API. Les profils anciens reçoivent des styles neutres ; les styles n’affectent pas la correction ordinaire.

## Génération RP (1.9)

Dans **Personnages → Génération RP**, choisissez un format (**Auto**, **/me**, **/do**, **/ame** ou **Entre étoiles**) et le niveau de détail (**Bref**, **Équilibré**, **Détaillé**). Des précisions facultatives complètent ces réglages. La génération emploie le style **Actions** et ses précisions, dans **Styles d’écriture** ; le style de dialogue reste indépendant. Enregistrez le personnage avant de jouer.

Sélectionnez votre idée puis utilisez **Ctrl + Alt + G** par défaut. Le raccourci est librement modifiable dans **Connexion & API**, comme les deux autres ; un clic droit permet de le désactiver. Le champ doit permettre Ctrl+C/Ctrl+V. L’outil propose un texte dans la sélection sans appuyer sur Entrée. Le mode **Génération** de l’onglet **Essai RP** permet d’essayer sans remplacer dans une autre application.

En Auto, l’IA choisit **/me** pour une action du personnage et **/do** pour un fait ou l’environnement. « Une odeur de fumée parce que j’ai brûlé la voiture » doit décrire l’odeur en /do, sans rejouer l’incendie. Une commande /me, /do ou /ame explicitement demandée dans la sélection est respectée en Auto ; un format imposé dans le profil est prioritaire.

La génération développe une idée et peut proposer de petits gestes du personnage ; ce n’est pas une reformulation. Les consignes limitent les ajouts de décor, de faits majeurs et les réactions imposées aux autres joueurs. L’IA peut encore ajouter un détail non souhaité : le niveau Bref ou des précisions ciblées permettent de resserrer les propositions. Le détail demandé est une indication de longueur, pas une obligation de remplir avec des inventions.

La biographie complète reste dans le profil et ses exports. Pour la génération, seuls les passages ayant des termes pertinents pour la sélection et le vocabulaire utile sont envoyés, afin d’éviter de glisser des éléments sans rapport avec la scène. Il ne s’agit pas d’une compréhension sémantique parfaite du contexte. Les accords, le nom, le style et les précisions du profil accompagnent chaque idée.

Un seul appel Groq utilise **GPT-OSS 120B**, avec la même clé et les mêmes budgets que la reformulation. Le budget total reste de dix secondes ; les secours ne se déclenchent que sur un dépassement de quota. Le texte sélectionné, les consignes, le contexte pertinent et la sortie consomment des jetons. Aucun abonnement ou paiement n’est activé.

Un contrôle distinct exige un seul message avec le bon préfixe RP ou la bonne paire d’étoiles et refuse les commandes supplémentaires, préambules et retours à la ligne. Il ne garantit pas le sens exact du texte. Les vérifications de fenêtre, de sélection, de personnage et de presse-papiers restent les mêmes avant remplacement. Les formats, niveaux, précisions et le troisième raccourci sont inclus dans les exports ; les clés restent exclues.

## Personnages et vocabulaire

Un personnage possède son nom, ses accords, son contexte et son dictionnaire. Le dictionnaire commun s’applique à tous les profils. Ajoutez uniquement les noms et mots intentionnels à conserver, une entrée par ligne.

L’IA décode les abréviations SMS et les écritures phonétiques selon la syntaxe : t peut être tu, t’es, tes, te ou t’ai ; bcp, trql et oklm peuvent être développés. Elle corrige aussi les mots collés, lettres inversées, homophones, accords, accents et ponctuation en conservant le registre oral, le franglais et les insultes.

Les actions entre astérisques se corrigent comme /me ou /do. Les marqueurs RP, bégaiements, interruptions, sauts de ligne et mots protégés sont contrôlés avant collage. Les entrées d’une seule lettre utilisent leur casse exacte et excluent les initiales élidées ou bégayées pour ne pas confondre le mot J avec j’vais. Une IA peut encore mal interpréter une intention ambiguë ; utilisez l’écran d’essai pour évaluer vos propres phrases.

Pour Qwen, un contexte dépassant 150 caractères est résumé une fois en une fiche courte gardée en mémoire ; la fiche est recalculée si le contexte change. Le contexte complet reste intact dans votre profil et les exports. Une description de 150 caractères maximum est transmise telle quelle. L’identité, les accords et le vocabulaire pertinent accompagnent chaque correction. Le résultat de l’essai affiche la fiche utilisée.

## Sélection dans le jeu

Sélectionnez le texte puis utilisez **Alt + Espace**. Laissez la sélection en place pendant la correction. Le logiciel copie, corrige, revérifie la fenêtre et la sélection, puis remplace le texte. Il n’appuie jamais sur Entrée. Le champ doit accepter Ctrl+C et Ctrl+V. Gardez Reply AI ouverte ou réduite ; la croix quitte le programme.

Le raccourci est modifiable dans les réglages. Le menu près de l’horloge permet de changer de personnage et quitter. Un diagnostic indique l’enregistrement Windows du raccourci et la dernière étape atteinte, sans journal de vos messages ni de vos clés.

Si Windows garde le presse-papiers occupé, les accès sont réessayés brièvement (900 ms maximum d’attente par opération) sans attente imposée en cas de succès immédiat. Un changement de fenêtre ou de presse-papiers interrompt le remplacement. La fermeture tente une restauration immédiate au mieux.

## Connexions Groq de secours

La liste des clés accepte jusqu’à huit secours en plus de la principale. Les cases et l’ordre du menu de chaque ligne contrôlent les tentatives. La principale est essayée d’abord ; les connexions décochées ne reçoivent pas le texte.

Seule une erreur HTTP 429 déclenche une nouvelle tentative avec la même sélection et le même personnage. Les erreurs de clé, de réseau, de serveur ou de contenu ne déclenchent pas de cascade. Le budget de correction est de **dix secondes pour toutes les tentatives réunies**, préparation du contexte comprise. Une réponse partielle n’est jamais collée.

Les clés d’une même organisation Groq partagent les quotas du modèle. Donnez-leur le même nom de groupe. Ce nom est déclaratif et ne crée pas de quota. Le groupe épuisé est mis en attente selon Retry-After, ou 60 secondes si absent ; les autres groupes éligibles peuvent être essayés. La même clé n’est pas rejouée sous une autre étiquette. Redémarrer l’application ne réinitialise pas le quota chez Groq.

Les tarifs et limites dépendent du modèle et du compte. Consultez [vos quotas](https://console.groq.com/settings/limits). Les consignes, le contexte et la réponse consomment également des jetons : le plafond de requêtes ne garantit pas autant de corrections disponibles. Les tests de connexion et la première préparation du contexte consomment aussi du quota.

## Import, export et stockage

Exportez un personnage ou l’espace complet depuis **Import & export**. Les clés et connexions de secours ne sont jamais incluses. À l’import, vérifiez l’aperçu avant de fusionner ou remplacer vos profils. Sur un nouveau PC, importez vos profils puis ajoutez les clés à nouveau.

Données locales dans `%LOCALAPPDATA%\ParoleRP` :

- `workspace.json` : personnages, dictionnaires et réglages ;
- `workspace.json.bak` : sauvegarde précédente ;
- `groq-key.protected` : clé principale chiffrée ;
- `connections.protected` : connexions Groq de secours chiffrées.

Partagez le ZIP de l’application, pas le dossier de vos données ni une copie manuelle de vos clés en clair.

## Reconstruction et vérification

`CONSTRUIRE.ps1` compile l’interface avec le compilateur .NET Framework Windows. `CONSTRUIRE-MOTEUR.ps1` installe les dépendances de `requirements-engine.txt` dans un environnement local et reconstruit le moteur Groq avec PyInstaller.

Les scripts `tests/VERIFIER.ps1`, `tests/VERIFIER-CLAVIER.ps1` et `tests/VERIFIER-PRESSE-PAPIERS.ps1` couvrent les données, migrations, protections RP, paquets clavier et accès temporaires au presse-papiers. `tests/test_python_engine.py` couvre les flux Groq, les délais, le contexte et les secours sur quota. Les basculements sont testés avec des erreurs simulées, sans épuiser volontairement les quotas d’un compte réel.

## Mises à jour

Dans Configurations → Mises à jour, cliquez sur Vérifier puis Mettre à jour si une version est disponible. Le téléchargement est vérifié avant fermeture ; Reply AI redémarre ensuite automatiquement. Les données locales restent à leur place. La version précédente des fichiers remplacés est conservée dans le dossier updates des données locales. Une connexion Internet à GitHub est nécessaire.

## Consommation et état des clés (2.2)

La page Consommation affiche les demandes de la session, du jour ou du mois, les jetons annoncés par Groq et la moyenne mesurée pour chaque fonction. Les appels de préparation du contexte sont inclus. Les échecs sans mesure sont signalés. L’historique usage.json ne contient que des agrégats par jour, sur 400 jours ; aucun texte, clé ni contexte. La session commence à l’ouverture de Reply AI ; aucun bouton de remise à zéro n’est affiché.

Les clés affichent séparément leur état de correction et leur état RP (reformulation et génération). Disponible décrit le dernier appel réussi. À vérifier signifie qu’aucun appel n’a encore confirmé ce modèle dans cette exécution. Un quota est partagé entre les clés de la même organisation et du même modèle. Le délai de réessai est approximatif ; aucun quota restant n’est inventé.

Le choix du modèle de correction se trouve dans Avancé. Reformulation et génération utilisent automatiquement GPT-OSS 120B avec les mêmes clés Groq.

Après un remplacement, Ctrl+Z peut restaurer le texte original pendant 30 secondes, tant que vous n’avez ni retouché le champ ni changé de fenêtre. Reply AI vérifie le texte avant de le rétablir et ne transmet aucune nouvelle demande à Groq. Après saisie, déplacement du curseur ou clic, Ctrl+Z reprend son fonctionnement habituel dans l’application cible. Si Ctrl+Z est déjà votre raccourci Reply AI ou si Windows refuse son enregistrement, l’annulation reste celle de l’application cible.

Les styles de dialogue Toxico et Crapule sont disponibles dans le profil du personnage. Ils conservent l’idée du message tout en changeant sa voix.

## Récupération du moteur (2.2.2)

Le délai de 10 secondes couvre toute la liaison avec le moteur, y compris les étapes d’envoi et de vidange du canal. Une liaison bloquée est retirée ; le prochain appui explicite peut démarrer un moteur neuf. Aucun renvoi automatique de la demande après une expiration.

Le fichier local diagnostic.log contient uniquement les réceptions de raccourcis, les étapes techniques, les résultats et les durées. Aucun message RP, contexte, nom de personnage, clé ni erreur brute n’y est écrit. Une seule copie précédente est conservée ; chaque journal est limité à environ 256 Kio. Ce fichier n’est pas inclus dans les exports de personnages. Il permet de différencier un raccourci reçu puis bloqué d’un délai du moteur.

Le presse-papiers des raccourcis, de Ctrl+Z et de la copie de résultat est traité par une instance auxiliaire STA sans interface. Ses appels sont limités à 2 secondes ; une opération bloquée ne retient plus le fil de l’interface. La récupération de l’ancien contenu reste conditionnée à la propriété du presse-papiers et peut échouer si son propriétaire Windows ne répond pas. Le journal technique indique les étapes de copie sans enregistrer le contenu.

Le processus STA auxiliaire surveille la fin du processus principal et quitte également si un appel Windows est bloqué.
