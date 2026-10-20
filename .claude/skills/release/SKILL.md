---
name: release
description: Quand l'utilisateur demande de push / publier, builder la Release de TestAgentMCP, zipper et créer la release GitHub (tag vX.Y.Z, un seul .zip, pas d'installeur). À utiliser à chaque « push ».
---

# Push + release GitHub

Convention (comme les autres dépôts) : tag `vMAJOR.MINOR.PATCH`, release GitHub du même nom, **un seul asset : le `.zip`**, pas d'installeur. Premier tag : `v1.0.0`, puis patch +1 (mineur/majeur seulement si l'utilisateur le dit).

**La clé OpenAI ne doit jamais être dans la build** : l'app la demande à l'utilisateur à l'envoi d'un prompt et la stocke chiffrée (DPAPI). Vérifier quand même (étape 3).

Depuis la racine du dépôt :

1. Committer ce qui doit l'être puis pousser : `git push origin main`.
2. Build Release (retirer d'abord un éventuel `output.log` du dossier de sortie, il serait zippé) :
```powershell
Remove-Item TestAgentMCP/bin/Release/net10.0-windows/output.log -ErrorAction SilentlyContinue
dotnet build TestAgentMCP/TestAgentMCP.csproj -c Release -p:NoExplorer=true
```
   Le zip est généré dans `TestAgentMCP/artifacts/TestAgentMCP-Release-net10.0-windows-<n>.zip` (n = `artifacts/zip-index.txt`).
3. Contrôle : le zip ne contient pas de clé et pas de `output.log`.
```powershell
$zip = Get-ChildItem TestAgentMCP/artifacts/*.zip | Sort-Object LastWriteTime | Select-Object -Last 1
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::OpenRead($zip.FullName).Entries.Name
Select-String -Path TestAgentMCP/bin/Release/net10.0-windows/TestAgentMCP.dll -Pattern 'sk-' -Encoding ascii -Quiet   # doit être False
```
4. Version suivante : `git tag --list "v*" --sort=-v:refname | Select-Object -First 1`.
5. Publier (renommer le zip en `TestAgentMCP-<tag>.zip` si on veut un nom propre) :
```powershell
gh release create vX.Y.Z $zip.FullName --title "vX.Y.Z" --notes "<résumé des changements>"
```
   `gh release create` crée aussi le tag sur le dernier commit poussé.
6. Rapporter : lien de la release, nom du zip, résultat du contrôle clé.
