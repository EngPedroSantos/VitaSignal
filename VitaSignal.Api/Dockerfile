FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["VitaSignal.Api/VitaSignal.Api.csproj", "VitaSignal.Api/"]
COPY ["VitaSignal.Application/VitaSignal.Application.csproj", "VitaSignal.Application/"]
COPY ["VitaSignal.Domain/VitaSignal.Domain.csproj", "VitaSignal.Domain/"]
COPY ["VitaSignal.Infrastructure/VitaSignal.Infrastructure.csproj", "VitaSignal.Infrastructure/"]
RUN dotnet restore "VitaSignal.Api/VitaSignal.Api.csproj"

COPY . .
WORKDIR /src/VitaSignal.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VitaSignal.Api.dll"]