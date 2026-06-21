# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura dependencias separadamente para aproveitar cache de camadas.
COPY ["ApiControlePerifericos/ApiControlePerifericos.csproj", "ApiControlePerifericos/"]
RUN dotnet restore "ApiControlePerifericos/ApiControlePerifericos.csproj"

# Copia o restante do codigo da API e publica.
COPY ApiControlePerifericos/ ApiControlePerifericos/
RUN dotnet publish "ApiControlePerifericos/ApiControlePerifericos.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cloud Run injeta a porta via env PORT (default 8080); a app tambem a le no Program.cs.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiControlePerifericos.dll"]
