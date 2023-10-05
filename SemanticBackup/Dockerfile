FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["SemanticBackup/SemanticBackup.csproj", "SemanticBackup/"]
COPY ["SemanticBackup.Core/SemanticBackup.Core.csproj", "SemanticBackup.Core/"]
RUN dotnet restore "SemanticBackup/SemanticBackup.csproj"
COPY . .
WORKDIR "/src/SemanticBackup"
RUN dotnet build "SemanticBackup.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SemanticBackup.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticBackup.dll"]