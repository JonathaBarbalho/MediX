# Instructions for Claude Code — MediX

## Commit language

All commit messages in this repository must be written in English, regardless of the language
used in the conversation with the user.

## Visibility

This repository must always stay public on GitHub.

## Public API documentation

Every public type and member must have an XML `/// <summary>` doc comment. The build enforces this:
`MediX.csproj` sets `GenerateDocumentationFile` and treats `CS1591` (missing XML comment on a
publicly visible member) as an error, so the build fails if a new public member ships without one.
These comments also ship inside the NuGet package (`GenerateDocumentationFile` + the packed `.xml`),
so they show up as IntelliSense for consumers — keep them accurate, not just present.

## NuGet publishing

Publishing to nuget.org is manual, not automated — there is no CI/CD publish pipeline, and none
should be added without the user explicitly asking for it. To publish a new version:

```
dotnet pack src/MediX/MediX.csproj -c Release -o ./artifacts
dotnet nuget push ./artifacts/MediX.<version>.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
```
