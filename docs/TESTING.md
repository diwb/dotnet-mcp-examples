# Testing

Validated commands in this pass:

```powershell
dotnet format DotNetMcpExamples.slnx --verify-no-changes --verbosity minimal
dotnet build DotNetMcpExamples.slnx -c Release --no-restore
dotnet test DotNetMcpExamples.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet run --project src/McpExamples.Client.Console --configuration Release -- stdio .\src\McpExamples.Server.Workspace\bin\Release\net10.0\McpExamples.Server.Workspace.exe tools
```

Result: 48 tests passed, 0 failed.

Coverage: 24.76% line coverage across Cobertura instrumented files.
