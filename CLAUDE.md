# TestAgentMCP

App WinForms (.NET 10) : canvas de formes (carrés, cercles, losanges) pilotable à la souris, par prompt (OpenAI) ou par MCP.

## Serveur MCP `shapes`

L'app doit être lancée : elle expose `http://localhost:3333/mcp` (déclaré dans `.mcp.json`, outils autorisés dans `.claude/settings.json`).

Outils (`mcp__shapes__*`) :
- `get_canvas_state` : lit la zone de dessin (`DrawingLeft/Top/Width/Height`) et les formes `{Id, X, Y, Size, Color, Shape}`.
- `apply_square_updates` : `action` = `Add` | `Update` | `Remove`, `infos` = liste de `{id, x, y, size, color, shape}`. Retourne le nouvel état.

Règles d'usage :
- Appelle toujours `get_canvas_state` d'abord (positions et ids actuels, limites).
- `Add` : l'id est ignoré (attribué automatiquement) ; x/y omis = position aléatoire ; taille par défaut 60 px.
- `Update` : seuls les champs fournis changent. `Remove` : seul l'id compte.
- `shape` : `Square`, `Circle` ou `Losange`. `color` : couleur HTML (`#FF0000`, `Red`).
- Les coordonnées sont limitées à la zone de dessin ; les formes hors zone sont ramenées dedans.
- Plusieurs formes en un seul appel quand c'est possible.

## Développement

- Build : `dotnet build TestAgentMCP/TestAgentMCP.csproj` (fermer l'app avant, sinon l'exe est verrouillé).
- Code : `MainForm.cs` (UI, état des formes), `LocalMcpProtocol.cs` (outils MCP + transport en mémoire), `HttpMcpHost.cs` (transport HTTP), `AgentMCP.cs` (agent OpenAI).
- La clé OpenAI vit dans `TestAgentMCP/OpenAi.props` (ignoré par git). Ne jamais la committer ni l'écrire dans le `.csproj`.
