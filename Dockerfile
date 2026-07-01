FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/WorldFaith.Shared/WorldFaith.Shared.csproj", "shared/WorldFaith.Shared/"]
COPY ["server/WorldFaith.Server/WorldFaith.Server.csproj", "server/WorldFaith.Server/"]
RUN dotnet restore "server/WorldFaith.Server/WorldFaith.Server.csproj"

COPY . .
WORKDIR "/src/server/WorldFaith.Server"
RUN dotnet build "WorldFaith.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WorldFaith.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorldFaith.Server.dll"]
