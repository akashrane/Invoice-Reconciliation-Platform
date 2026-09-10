FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ReconFlow.slnx ./
COPY backend/ReconFlow.Core/ReconFlow.Core.csproj backend/ReconFlow.Core/
COPY backend/ReconFlow.Application/ReconFlow.Application.csproj backend/ReconFlow.Application/
COPY backend/ReconFlow.Infrastructure/ReconFlow.Infrastructure.csproj backend/ReconFlow.Infrastructure/
COPY backend/ReconFlow.Api/ReconFlow.Api.csproj backend/ReconFlow.Api/
RUN dotnet restore backend/ReconFlow.Api/ReconFlow.Api.csproj
COPY backend/ backend/
RUN dotnet publish backend/ReconFlow.Api/ReconFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReconFlow.Api.dll"]

