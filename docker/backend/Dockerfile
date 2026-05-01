FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
COPY backend/ ./

RUN PROJECT=$(find . -maxdepth 1 -name '*.csproj' | head -n1) \
	&& if [ -z "$PROJECT" ]; then \
		echo 'No .csproj found in /app. Add backend/*.csproj to run the API container.' && exit 1; \
	else \
		dotnet restore "$PROJECT"; \
	fi

EXPOSE 8080

CMD ["sh", "-c", "PROJECT=$(find . -maxdepth 1 -name '*.csproj' | head -n1); dotnet run --no-restore --project \"$PROJECT\" --urls http://0.0.0.0:8080"]
