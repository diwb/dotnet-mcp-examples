# Deployment

The HTTP server can be built with Docker:

```powershell
docker build -t dotnet-mcp-examples-remote .
docker compose up --build
```

The container listens on port `8080` and exposes `/health` separately from `/mcp`.
