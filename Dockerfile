FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PokemonBattle/PokemonBattle.csproj PokemonBattle/
RUN dotnet restore PokemonBattle/PokemonBattle.csproj

COPY PokemonBattle/ PokemonBattle/
RUN dotnet publish PokemonBattle/PokemonBattle.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:$PORT exec dotnet PokemonBattle.dll"]