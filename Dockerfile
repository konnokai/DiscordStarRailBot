#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DiscordStarRailBot/DiscordStarRailBot.csproj", "DiscordStarRailBot/"]
RUN dotnet restore "DiscordStarRailBot/DiscordStarRailBot.csproj"
COPY . .
WORKDIR "/src/DiscordStarRailBot"
RUN dotnet build "DiscordStarRailBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordStarRailBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .

ENV TZ="Asia/Taipei"

STOPSIGNAL SIGQUIT

ENTRYPOINT ["dotnet", "DiscordStarRailBot.dll"]