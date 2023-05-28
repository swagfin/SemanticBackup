FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["SemanticBackup.API/SemanticBackup.API.csproj", "SemanticBackup.API/"]
COPY ["SemanticBackup.Core/SemanticBackup.Core.csproj", "SemanticBackup.Core/"]
RUN dotnet restore "SemanticBackup.API/SemanticBackup.API.csproj"
COPY . .
WORKDIR "/src/SemanticBackup.API"
RUN dotnet build "SemanticBackup.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SemanticBackup.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticBackup.API.dll"]