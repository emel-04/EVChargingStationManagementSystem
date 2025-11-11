FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/services/StationService/StationService.csproj", "src/services/StationService/"]
COPY ["src/shared/Shared/Shared.csproj", "src/shared/Shared/"]
RUN dotnet restore "src/services/StationService/StationService.csproj"
COPY . .
WORKDIR "/src/src/services/StationService"
RUN dotnet build "StationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StationService.dll"]






