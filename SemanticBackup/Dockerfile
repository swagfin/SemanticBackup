FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SemanticBackup/SemanticBackup.csproj", "SemanticBackup/"]
COPY ["SemanticBackup.Core/SemanticBackup.Core.csproj", "SemanticBackup.Core/"]
COPY ["SemanticBackup.Infrastructure/SemanticBackup.Infrastructure.csproj", "SemanticBackup.Infrastructure/"]
RUN dotnet restore "./SemanticBackup/./SemanticBackup.csproj"
COPY . .
WORKDIR "/src/SemanticBackup"
RUN dotnet build "./SemanticBackup.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SemanticBackup.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticBackup.dll"]