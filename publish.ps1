# Hebt die Version in Radzen.Blazor.csproj an, committet und taggt sie (v<Version>) und schiebt
# Commit + Tag nach git.gauss-lvs.de. Der Tag-Push startet dort den Workflow
# .forgejo/workflows/publish-nuget.yml, der baut, testet und Paket samt Symbolen
# nach nuget.gauss-lvs.de veroeffentlicht.
# --follow-tags ist noetig: ohne die Option bleibt der annotierte Tag lokal und es baut nichts.
dotnet version -f ./Radzen.Blazor/Radzen.Blazor.csproj build `
  && git push --follow-tags
