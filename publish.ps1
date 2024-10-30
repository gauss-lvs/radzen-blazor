dotnet version -f .\Radzen.Blazor\Radzen.Blazor.csproj build `
  && dotnet pack .\Radzen.Blazor\Radzen.Blazor.csproj -o tmp_pack -c Release `
  && Copy-Item "tmp_pack/*.nupkg" $env:GAUSS_NUGET_FOLDER `
  && dotnet nuget push "tmp_pack/*.nupkg" --source gauss --api-key $env:GAUSS_NUGET_API_KEY --skip-duplicate `
  && Remove-Item tmp_pack -Recurse `
  && git push