---
name: add-mcp-tool
description: Ajouter, renommer ou modifier un outil du serveur MCP `shapes` (get_canvas_state, apply_square_updates, set_animation, get_help…) en gardant synchronisés code, docs MCP, sous-agent, README et CLAUDE.md. À utiliser dès qu'on touche au contrat d'un outil MCP (nom, paramètres, comportement).
---

# Ajouter / modifier un outil MCP `shapes`

Un outil MCP est réparti sur plusieurs fichiers qui doivent rester cohérents. Parcours toute la checklist, dans l'ordre. Ne saute pas une étape sous prétexte que « c'est petit » : les oublis ne cassent pas la build, ils laissent juste les agents avec une doc fausse.

## 1. Définition de l'outil
`TestAgentMCP/Mcp/Common/McpToolDefinitions.cs`
- Ajouter un champ `public static readonly object <Nom> = new { name = "snake_case", description = ..., inputSchema = ... }`.
- Réutiliser `ShapeInfoProperties` pour les infos de forme.
- Ajouter l'outil au tableau `All` (sinon `tools/list` ne l'expose pas).
- La `description` est lue par les agents : elle doit se suffire à elle-même (règles clés, valeurs par défaut, renvoi vers `get_help`).

## 2. Traitement de l'appel
`TestAgentMCP/Mcp/Common/McpServer.cs`
- Ajouter une branche `string.Equals(name, "...", OrdinalIgnoreCase)` dans `HandleToolsCall`.
- Si l'outil agit sur l'app, ajouter un callback au constructeur primaire (`Action<...>`, optionnel avec `= null` si certains transports ne le supportent pas, et renvoyer `BuildError(id, -32602, ...)` quand il est nul, comme `set_animation`).
- Retourner l'état via `BuildTextResult(id, JsonSerializer.Serialize(_stateProvider(), _jsonOptions))` si l'outil modifie le canvas.
- Nouveaux modèles de données : `Mcp/Common/CanvasModels.cs`.

## 3. Câblage dans l'app
- Transport HTTP : `StartHttpMcpServer()` dans `TestAgentMCP/Forms/MainForm.cs` crée le `McpServer` et marshale chaque callback sur le thread UI avec `Invoke(...)` ; appeler `RecordMcpCall("...")` pour le résumé d'activité. Logique métier dans un partial `MainForm.*.cs` (pas dans le Designer).
- Transport local (agent OpenAI) : `Mcp/Local/LocalMcpClient.cs` n'expose que `get_canvas_state` et `apply_square_updates`, et le `McpServer` local est construit sans `setAnimation`. N'y toucher que si l'outil doit être disponible pour l'agent OpenAI.
- Contrôles visibles : toujours déclarés dans `MainForm.Designer.cs`, jamais `new Button()` en code (voir CLAUDE.md).

## 4. Documentation servie par le MCP
`TestAgentMCP/Mcp/Common/McpDocs.cs`
- Mettre à jour `Instructions` (envoyé à l'initialisation) et les docs `guide` / `animation` / `examples` si le comportement change.
- Un nouveau topic = nouvelle entrée dans `All` ; `TopicList`, l'enum de `get_help` et les resources `shapes://docs/{topic}` suivent automatiquement.

## 5. Sous-agent et permissions
- `.claude/agents/shapes-agent.md` : ajouter `mcp__shapes__<nom>` à la ligne `tools:` (l'agent est restreint à une liste explicite). Si l'outil change la méthode de travail, ajuster le corps du prompt, sans y mettre de connaissance du code : l'agent ne doit rien savoir de l'app hors MCP.
- `.claude/settings.json` : `"allow": ["mcp__shapes"]` autorise déjà tout le serveur, rien à changer. Ne le modifier que si ce motif change.
- `.mcp.json` : URL `http://localhost:3333/mcp`, inchangée sauf changement de port (`HttpMcpPort` dans `MainForm.cs`).

## 6. Docs du dépôt
- `CLAUDE.md`, section « Outils » : une ligne par outil, mêmes champs que dans la définition.
- `README.md` : la liste d'outils (autour de `apply_square_updates` / `set_animation`) et la ligne décrivant `Mcp/Common/`.
- Les trois sources doivent dire la même chose : définition (C#), `McpDocs`, README/CLAUDE.md.

## 7. Vérification
1. Utiliser le skill `build-run` pour compiler et relancer l'app (l'exe est verrouillé tant qu'elle tourne).
2. Les outils du serveur ne sont rechargés côté Claude Code qu'après reconnexion du serveur `shapes` ; sinon la session voit l'ancienne liste.
3. Appeler `get_canvas_state`, puis le nouvel outil, et contrôler la réponse. Pour tester sans a priori, déléguer au sous-agent `shapes-agent` : s'il réussit avec la seule description de l'outil, la doc est suffisante.
4. Rapporter ce qui a été vérifié et ce qui ne l'a pas été (par exemple transport local OpenAI non testé).

## Pièges connus
- Oublier d'ajouter l'outil à `McpToolDefinitions.All` : il répond mais n'est jamais listé.
- Oublier `shapes-agent.md` : le sous-agent ne voit pas l'outil.
- Appeler du code UI depuis le callback sans `Invoke` : les requêtes HTTP arrivent sur des threads du pool.
- Ne jamais mettre la clé OpenAI ailleurs que dans `TestAgentMCP/OpenAi.props`.
