# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura dependencias separadamente para aproveitar cache de camadas.
# Clean Architecture: o WebApi referencia Domain/Application/Infrastructure,
# entao todos os .csproj precisam estar presentes antes do restore.
COPY ["ApiControlePerifericos/ApiControlePerifericos.csproj", "ApiControlePerifericos/"]
COPY ["ApiControlePerifericos.Domain/ApiControlePerifericos.Domain.csproj", "ApiControlePerifericos.Domain/"]
COPY ["ApiControlePerifericos.Application/ApiControlePerifericos.Application.csproj", "ApiControlePerifericos.Application/"]
COPY ["ApiControlePerifericos.Infrastructure/ApiControlePerifericos.Infrastructure.csproj", "ApiControlePerifericos.Infrastructure/"]
RUN dotnet restore "ApiControlePerifericos/ApiControlePerifericos.csproj"

# Copia o restante do codigo (todas as camadas) e publica.
COPY ApiControlePerifericos/ ApiControlePerifericos/
COPY ApiControlePerifericos.Domain/ ApiControlePerifericos.Domain/
COPY ApiControlePerifericos.Application/ ApiControlePerifericos.Application/
COPY ApiControlePerifericos.Infrastructure/ ApiControlePerifericos.Infrastructure/

# O MANUAL.md e a fonte unica do assistente e e referenciado como Content pelo csproj
# do WebApi (..\MANUAL.md). Sem esta copia o publish falha por arquivo ausente.
COPY MANUAL.md .

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
