# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore (better layer caching)
COPY ["TransactionGateway.API/TransactionGateway.API.csproj", "TransactionGateway.API/"]
RUN dotnet restore "TransactionGateway.API/TransactionGateway.API.csproj"

# Copy everything else and publish
COPY . .
WORKDIR "/src/TransactionGateway.API"
RUN dotnet publish "TransactionGateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TransactionGateway.API.dll"]