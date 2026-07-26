# Testing

Validated commands in this pass:

```powershell
dotnet restore DotNetMcpExamples.slnx
dotnet build DotNetMcpExamples.slnx -c Release --no-restore
dotnet test DotNetMcpExamples.slnx -c Release --no-build
dotnet run --project src/McpExamples.Client.Console --configuration Release -- doctor
dotnet run --project src/McpExamples.Client.Console --configuration Release -- stdio .\src\McpExamples.Server.Workspace\bin\Release\net10.0\McpExamples.Server.Workspace.exe initialize "{}"
```

Result: 9 tests passed across unit, protocol, security and integration projects.
