FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Dourak.sln .
COPY src/Dourak.Domain/Dourak.Domain.csproj src/Dourak.Domain/
COPY src/Dourak.Application/Dourak.Application.csproj src/Dourak.Application/
COPY src/Dourak.Infrastructure/Dourak.Infrastructure.csproj src/Dourak.Infrastructure/
COPY src/Dourak.Api/Dourak.Api.csproj src/Dourak.Api/
COPY tests/Dourak.Domain.Tests/Dourak.Domain.Tests.csproj tests/Dourak.Domain.Tests/
COPY tests/Dourak.Application.Tests/Dourak.Application.Tests.csproj tests/Dourak.Application.Tests/
RUN dotnet restore src/Dourak.Api/Dourak.Api.csproj

COPY . .
RUN dotnet publish src/Dourak.Api/Dourak.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dourak.Api.dll"]
