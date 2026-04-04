FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/frontend/EVChargingStation.Web/EVChargingStation.Web.csproj", "src/frontend/EVChargingStation.Web/"]
COPY ["src/shared/Shared/Shared.csproj", "src/shared/Shared/"]
RUN dotnet restore "src/frontend/EVChargingStation.Web/EVChargingStation.Web.csproj"
COPY . .
WORKDIR "/src/src/frontend/EVChargingStation.Web"
RUN dotnet build "EVChargingStation.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EVChargingStation.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EVChargingStation.Web.dll"]





