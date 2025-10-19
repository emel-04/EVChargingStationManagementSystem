FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/services/IdentityService/IdentityService.csproj", "src/services/IdentityService/"]
COPY ["src/services/Common/Common.csproj", "src/services/Common/"]
RUN dotnet restore "src/services/IdentityService/IdentityService.csproj"
COPY . .
WORKDIR "/src/src/services/IdentityService"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]