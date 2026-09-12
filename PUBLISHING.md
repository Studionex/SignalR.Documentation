# Publishing

Build and test from this solution directory:

```powershell
dotnet test .\SignalR.Documentation.Tests -c Release
dotnet pack .\SignalR.Documentation\SignalR.Documentation.csproj -c Release -o .\artifacts
```

Before public publication, set the final PackageId, Version and Authors in the library project. Package IDs must be available on the target feed. Set license and repository metadata if applicable.

## Upload with the website

Sign in at https://www.nuget.org and open https://www.nuget.org/packages/manage/upload.
Select `artifacts/SignalR.Documentation.1.0.0.nupkg`, review the metadata, and publish.

## Upload with the CLI

Create a scoped API key in your NuGet.org account. Keep it out of source files and chat. Set it locally in the NUGET_API_KEY environment variable, then run:

```powershell
dotnet nuget push .\artifacts\SignalR.Documentation.1.0.0.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

Use a new version number for each subsequent release.
