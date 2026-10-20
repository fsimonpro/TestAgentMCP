---
name: build-run
description: Compiler TestAgentMCP, relancer l'app WinForms et vérifier que le serveur MCP répond sur http://localhost:3333/mcp. À utiliser pour builder, redémarrer l'app après une modif, ou diagnostiquer « shapes ECONNREFUSED » / exe verrouillé.
---

# Build + relance + contrôle du serveur MCP

Fichiers : projet `TestAgentMCP/TestAgentMCP.csproj`, exe Debug `TestAgentMCP/bin/Debug/net10.0-windows/TestAgentMCP.exe`, processus `TestAgentMCP`, MCP sur `http://localhost:3333/mcp`.

Commandes PowerShell, depuis la racine du dépôt (`F:\Dev\TestAgentMCP`).

## 1. État initial
```powershell
Get-Process TestAgentMCP -ErrorAction SilentlyContinue | Select-Object Id, MainWindowTitle, Path
```
- Si l'app tourne, l'exe est verrouillé : la build échoue tant qu'elle n'est pas fermée.
- Le canvas n'est pas persistant : fermer l'app perd les formes et l'animation en cours. Si le travail peut compter pour l'utilisateur, le dire avant (il peut sauvegarder un preset depuis l'UI).
- Fermer proprement d'abord, forcer seulement si elle ne répond pas :
```powershell
$p = Get-Process TestAgentMCP -ErrorAction SilentlyContinue
if ($p) { $null = $p.CloseMainWindow(); if (-not $p.WaitForExit(5000)) { $p | Stop-Process -Force } }
```
- Si l'app a été lancée depuis Visual Studio (débogage attaché), demander à l'utilisateur de l'arrêter depuis VS plutôt que de la tuer.

## 2. Build
```powershell
dotnet build TestAgentMCP/TestAgentMCP.csproj
```
Lire la fin de la sortie : erreurs et nombre d'avertissements. Si la build échoue :
- `MSB3577` (deux `.resx` pour `MainForm`) : un partial `MainForm.*.cs` a généré un `.resx` associé. Supprimer ce `.resx` et ne pas ouvrir les partials en vue Conception (voir CLAUDE.md).
- Fichier verrouillé : l'app ou VS tient encore l'exe, retour à l'étape 1.
- Clé OpenAI : jamais dans la build ni dans le code ; l'app la demande au premier prompt et la stocke chiffrée (`ApiKeyStore`).

## 3. Relance
```powershell
Start-Process -FilePath "TestAgentMCP/bin/Debug/net10.0-windows/TestAgentMCP.exe" -WorkingDirectory "TestAgentMCP/bin/Debug/net10.0-windows"
```
Utiliser `-c Release` + le dossier `bin/Release/...` seulement si l'utilisateur le demande.

## 4. Contrôle du serveur MCP
Le serveur démarre à l'événement `Shown` de la fenêtre : laisser 2 à 3 secondes puis tester un `initialize` :
```powershell
$body = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}'
Invoke-RestMethod -Uri http://localhost:3333/mcp -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 5
```
Réponse attendue : `serverInfo.name = SquareMcpServer`. Si la connexion est refusée :
- Port 3333 déjà pris : `Get-NetTCPConnection -LocalPort 3333 -ErrorAction SilentlyContinue`. L'app affiche alors « Serveur MCP HTTP non démarré » dans son statut (échec loggué `MCP_HTTP` / `ERROR`).
- App plantée au démarrage : relancer et regarder la fenêtre ou les logs.

## 5. Reconnexion côté Claude Code
Si la session affichait `shapes (ECONNREFUSED)` ou si la liste d'outils a changé, le serveur `shapes` doit être reconnecté : depuis l'app Claude Desktop, utiliser le panneau des connecteurs / `reconnect_session_connector`, ou, dans un terminal `claude`, `/mcp`. Ensuite `mcp__shapes__get_canvas_state` doit répondre.

## 6. Rapport
Dire en clair : build réussie ou non (erreurs et avertissements), app relancée, MCP joignable ou non, et ce qui n'a pas été vérifié (par exemple outils `shapes` pas encore reconnectés dans la session).
