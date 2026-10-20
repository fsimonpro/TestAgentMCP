# TestAgentMCP

App WinForms (.NET 10) : canvas de formes (carrés, cercles, losanges) pilotable à la souris, par prompt (OpenAI) ou par MCP.

## Serveur MCP `shapes`

L'app doit être lancée : elle expose `http://localhost:3333/mcp` (déclaré dans `.mcp.json`, outils autorisés dans `.claude/settings.json`).

Outils (`mcp__shapes__*`) :
- `get_canvas_state` : lit la zone de dessin (`DrawingLeft/Top/Width/Height`) et les formes `{Id, X, Y, Size, Color, Shape}`.
- `apply_square_updates` : `action` = `Add` | `Update` | `Remove`, `infos` = liste de `{id, x, y, size, color, shape}`. Retourne le nouvel état.

- `set_animation` : `frames` = liste (2 à 50) de listes de `{id, x, y, size, color, shape}`, `fps` optionnel (défaut 15). Chaque frame s'applique en Update sur la précédente (la 1re part de l'état courant). Joue en boucle avec un lecteur (slider, lecture/stop) ; `frames: []` supprime l'animation. `get_canvas_state` expose `FrameCount/CurrentFrame/IsPlaying/Fps`.

App : Ctrl+Z annule (10 états max, y compris les modifs MCP) ; en animation, l'édition manuelle modifie la frame affichée. Le lecteur a « + » (insère une copie de la frame affichée après elle) et « 🗑 » (supprime la frame affichée ; à 1 frame restante l'animation se termine).

Règles d'usage :
- Appelle toujours `get_canvas_state` d'abord (positions et ids actuels, limites).
- `Add` : l'id est ignoré (attribué automatiquement) ; x/y omis = position aléatoire ; taille par défaut 60 px.
- `Update` : seuls les champs fournis changent. `Remove` : seul l'id compte.
- `shape` : `Square`, `Circle` ou `Losange`. `color` : couleur HTML (`#FF0000`, `Red`).
- Les coordonnées sont limitées à la zone de dessin ; les formes hors zone sont ramenées dedans.
- Plusieurs formes en un seul appel quand c'est possible.

Documentation servie par le MCP (`Mcp/Common/McpDocs.cs`) : `instructions` à l'initialisation, resources `shapes://docs/{guide,animation,examples}` et outil `get_help(topic)`. Mettre à jour ces textes quand le comportement des outils change.

Sous-agent `shapes-agent` (`.claude/agents/shapes-agent.md`) : limité aux 4 outils `mcp__shapes__*`, sans accès au code ni aux fichiers ; il ne connaît l'app que par ce que le serveur MCP expose. À déléguer pour dessiner/animer.

## Développement

- Build : `dotnet build TestAgentMCP/TestAgentMCP.csproj` (fermer l'app avant, sinon l'exe est verrouillé).
- Code : `Forms/MainForm.cs` (UI, état des formes), `Mcp/Common/` (serveur MCP, définitions d'outils, modèles, docs : partagés), `Mcp/Local/` (client + transport en mémoire, agent OpenAI), `Mcp/Remote/HttpMcpHost.cs` (transport HTTP), `Agent/AgentMCP.cs` (agent OpenAI).
- **UI = Designer** : tout contrôle visible (boutons, panneaux, menus, timers…) est déclaré dans `Forms/MainForm.Designer.cs` (`InitializeComponent`, champs sans `_`) pour apparaître dans la vue Conception de Visual Studio. Jamais de `new Button()`/`new Panel()` en code dans `MainForm.cs` ou les autres partials ; ils ne contiennent que la logique et le câblage des événements. Seuls les contrôles purement dynamiques (créés selon les données) peuvent être faits en code.
- Les partials `MainForm.*.cs` sont de la logique, pas des formulaires : ne pas les ouvrir en vue Conception ni laisser créer de `.resx` associé (deux `.resx` sur le même `MainForm` cassent la build : MSB3577).
- La clé OpenAI n'est jamais dans la build (ni Debug ni Release), ni dans le `.csproj`/le code. L'app la demande à l'envoi d'un prompt (`EnsureApiKey`, `ApiKeyDialog`) et la stocke chiffrée (DPAPI) dans `%LocalAppData%\TestAgentMCP\openai.key` (`ApiKeyStore`) ; `OPENAI_API_KEY` est aussi lue.

## Push / release

« Push » = pousser puis publier une release GitHub (skill `release` : tag `vX.Y.Z`, un seul `.zip`, pas d'installeur). La clé OpenAI n'est jamais embarquée (voir Développement).
