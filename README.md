# TestAgentMCP

App WinForms de formes (carrés, cercles, losanges) pilotable par prompt (OpenAI) ou par un client MCP externe comme Claude Code.

## Serveur MCP

Au démarrage, l'app expose un serveur MCP (Streamable HTTP) sur `http://localhost:3333/mcp`, accessible uniquement en local.

Outils :
- `get_canvas_state` : lit les limites du canvas et la liste des formes.
- `apply_square_updates` : `action` = `Add` | `Update` | `Remove`, `infos` = liste de `{id, x, y, size, color, shape}`. Retourne le nouvel état.

Connexion depuis Claude Code (app lancée) :

```bash
claude mcp add --transport http squares http://localhost:3333/mcp
```

Le fichier `.mcp.json` à la racine fait la même chose pour ce projet.

## Clé OpenAI (agent par prompt)

Copier `TestAgentMCP/OpenAi.props.example` vers `TestAgentMCP/OpenAi.props` (ignoré par git) et y mettre la clé.
