FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/ProductDetailService/ProductDetailService.csproj", "src/ProductDetailService/"]
RUN dotnet restore "src/ProductDetailService/ProductDetailService.csproj"
COPY . .
WORKDIR "/src/src/ProductDetailService"
RUN dotnet build "ProductDetailService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductDetailService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductDetailService.dll"]
