dotnet version -f .\Radzen.Blazor\Radzen.Blazor.csproj build `
  && dotnet pack .\Radzen.Blazor\Radzen.Blazor.csproj -o ".nupkgs" -c Release `
  && Copy-Item ".nupkgs\*.nupkg" $env:GAUSS_NUGET_FOLDER `
  && dotnet nuget push ".nupkgs\*.nupkg" --source gauss --api-key $env:GAUSS_NUGET_API_KEY --skip-duplicate `
  && Remove-Item ".nupkgs" -Recurse `
  && git push