FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/OrderOrchestratorService/OrderOrchestratorService.csproj", "src/OrderOrchestratorService/"]
RUN dotnet restore "src/OrderOrchestratorService/OrderOrchestratorService.csproj"
COPY . .
WORKDIR "/src/src/OrderOrchestratorService"
RUN dotnet build "OrderOrchestratorService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderOrchestratorService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderOrchestratorService.dll"]
