# See https://aka.ms/customizecontainer to learn how to customize your debug container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update && apt-get install -y --no-install-recommends \
    nodejs \
    && npm install -g @google/gemini-cli \
    && apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    && pip3 install --break-system-packages google-generativeai \
    && apt-get install -y --no-install-recommends \
    procps \
    lm-sensors \
    scrot \
    htop \
    ripgrep \
    && ln -sf /usr/bin/rg /usr/bin/ripgrep \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MyLinuxBot.csproj", "."]
RUN dotnet restore "./MyLinuxBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./MyLinuxBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MyLinuxBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyLinuxBot.dll"]
