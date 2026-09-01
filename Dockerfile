FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PokemonBattle/PokemonBattle.csproj PokemonBattle/
RUN dotnet restore PokemonBattle/PokemonBattle.csproj

COPY PokemonBattle/ PokemonBattle/
RUN dotnet publish PokemonBattle/PokemonBattle.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet PokemonBattle.dll --urls http://0.0.0.0:${PORT:-10000}"]