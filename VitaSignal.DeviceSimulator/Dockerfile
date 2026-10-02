FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", ".editorconfig", "./"]
COPY ["VitaSignal.DeviceSimulator/VitaSignal.DeviceSimulator.csproj", "VitaSignal.DeviceSimulator/"]
RUN dotnet restore "VitaSignal.DeviceSimulator/VitaSignal.DeviceSimulator.csproj"

COPY VitaSignal.DeviceSimulator/ VitaSignal.DeviceSimulator/
RUN dotnet publish "VitaSignal.DeviceSimulator/VitaSignal.DeviceSimulator.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "VitaSignal.DeviceSimulator.dll"]
