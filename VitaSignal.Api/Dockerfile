FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", ".editorconfig", "./"]
COPY ["VitaSignal.Api/VitaSignal.Api.csproj", "VitaSignal.Api/"]
COPY ["VitaSignal.Application/VitaSignal.Application.csproj", "VitaSignal.Application/"]
COPY ["VitaSignal.Domain/VitaSignal.Domain.csproj", "VitaSignal.Domain/"]
COPY ["VitaSignal.Infrastructure/VitaSignal.Infrastructure.csproj", "VitaSignal.Infrastructure/"]
RUN dotnet restore "VitaSignal.Api/VitaSignal.Api.csproj"

COPY VitaSignal.Domain/ VitaSignal.Domain/
COPY VitaSignal.Application/ VitaSignal.Application/
COPY VitaSignal.Infrastructure/ VitaSignal.Infrastructure/
COPY VitaSignal.Api/ VitaSignal.Api/
RUN dotnet publish "VitaSignal.Api/VitaSignal.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "VitaSignal.Api.dll"]
