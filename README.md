# ECN Manager — Full Repo PRO

## Run (Web)
dotnet restore
dotnet run --project src/WebApp/WebApp.csproj --urls http://0.0.0.0:5000
Open http://localhost:5000/index.html

Demo logins: U004/bao (Admin), U001/minh, U002/lan, U003/quang

## Desktop
dotnet publish src/WebApp/WebApp.csproj -c Release -o src/WebApp/bin/Release/net8.0/publish
dotnet publish src/DesktopHost/DesktopHost.csproj -c Release -o out/desktop
Run out/desktop/DesktopHost.exe (requires WebApp running at 127.0.0.1:5000)

## IIS
Use artifact `WebApp-publish` from Actions or:
dotnet publish src/WebApp/WebApp.csproj -c Release -o publish
Point IIS to ./publish (see build/iis/web.config)

## AI
POST /api/ai/ask { question } — searches AI_KB & ECNMaster (local RAG)
