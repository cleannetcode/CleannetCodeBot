FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CleannetCodeBot.Twitch/CleannetCodeBot.Twitch.csproj", "CleannetCodeBot.Twitch/"]
RUN dotnet restore "CleannetCodeBot.Twitch/CleannetCodeBot.Twitch.csproj"
COPY . .
WORKDIR "/src/CleannetCodeBot.Twitch"
RUN dotnet build "CleannetCodeBot.Twitch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CleannetCodeBot.Twitch.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleannetCodeBot.Twitch.dll"]
