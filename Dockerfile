#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.100 AS build
WORKDIR /src
COPY ["CardStorageRestAPI.csproj", "."]
RUN dotnet restore "./CardStorageRestAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "CardStorageRestAPI.csproj" -c Release -o /app/build

FROM build AS publish
ARG VERSION="0.0.0"
RUN dotnet publish "CardStorageRestAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:Version="$VERSION"

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CardStorageRestAPI.dll"]