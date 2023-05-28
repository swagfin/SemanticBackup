#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["SemanticBackup.WebClient/SemanticBackup.WebClient.csproj", "SemanticBackup.WebClient/"]
RUN dotnet restore "SemanticBackup.WebClient/SemanticBackup.WebClient.csproj"
COPY . .
WORKDIR "/src/SemanticBackup.WebClient"
RUN dotnet build "SemanticBackup.WebClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SemanticBackup.WebClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticBackup.WebClient.dll"]