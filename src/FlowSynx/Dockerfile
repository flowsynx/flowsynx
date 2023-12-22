#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/FlowSync/FlowSynx.csproj", "src/FlowSync/"]
COPY ["src/FlowSync.Core/FlowSynx.Core.csproj", "src/FlowSync.Core/"]
COPY ["plugins/AzureBlob/FlowSynx.Storage.Azure.Blob/FlowSynx.Storage.Azure.Blob.csproj", "plugins/AzureBlob/FlowSynx.Storage.Azure.Blob/"]
COPY ["plugins/LocalFileSystem/FlowSynx.Storage.LocalFileSystem/FlowSynx.Storage.LocalFileSystem.csproj", "plugins/LocalFileSystem/FlowSynx.Storage.LocalFileSystem/"]
RUN dotnet restore "./src/FlowSync/./FlowSynx.csproj"
COPY . .
WORKDIR "/src/src/FlowSync"
RUN dotnet build "./FlowSynx.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FlowSynx.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlowSynx.dll"]