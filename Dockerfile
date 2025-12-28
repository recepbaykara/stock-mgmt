FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["StockMgmt.csproj", "./"]
RUN dotnet restore "StockMgmt.csproj"

COPY . .
RUN dotnet build "StockMgmt.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "StockMgmt.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM build AS migrations

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet ef migrations bundle \
    --project StockMgmt.csproj \
    --startup-project StockMgmt.csproj \
    --configuration Release \
    --output /app/efbundle

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=migrations /app/efbundle /app/efbundle

RUN chmod +x /app/efbundle

ENTRYPOINT ["dotnet", "StockMgmt.dll"]
