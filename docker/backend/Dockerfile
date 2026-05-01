FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
COPY backend/ ./

EXPOSE 8080

# This repo currently has no .csproj/.sln. The container stays alive with a clear
# message until a backend project file is added.
CMD ["sh", "-c", "PROJECT=$(find . -maxdepth 1 -name '*.csproj' | head -n1); if [ -z \"$PROJECT\" ]; then echo 'No .csproj found in /app. Add backend/*.csproj to run the API container.'; tail -f /dev/null; else dotnet restore \"$PROJECT\" && dotnet run --project \"$PROJECT\" --urls http://0.0.0.0:8080; fi"]
