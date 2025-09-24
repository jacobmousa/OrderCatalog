# Catalog API Dockerfile (multi-stage build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution & props
COPY ECommerceDemo.sln ./
COPY Directory.Build.props ./

# Copy project files first for restore layer caching
COPY src/Catalog.Api/Catalog.Api.csproj src/Catalog.Api/
COPY src/Orders.Api/Orders.Api.csproj src/Orders.Api/

RUN dotnet restore src/Catalog.Api/Catalog.Api.csproj

# Copy everything
COPY . .
RUN dotnet publish src/Catalog.Api/Catalog.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
