# ECN Manager — Full Repo (minimal working)

## Run web (dev)
dotnet restore
dotnet run --project src/WebApp/WebApp.csproj --urls http://0.0.0.0:5000

Demo accounts: U004/bao, U001/minh, U002/lan, U003/quang

## Desktop
dotnet build src/DesktopHost/DesktopHost.csproj -c Release

## Notes
- Controllers/Services/Data stub sẵn, compile được và có /auth/login, /api/ecn, /api/ai/ask.
- wwwroot/ecn.html đã gắn AI module 1 chiếc (aiInput/aiSend/aiOutput) + adapter.