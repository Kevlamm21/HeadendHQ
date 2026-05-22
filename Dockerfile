FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/HeadendHQ.Web/HeadendHQ.Web.csproj", "src/HeadendHQ.Web/"]
COPY ["src/HeadendHQ.Core/HeadendHQ.Core.csproj", "src/HeadendHQ.Core/"]
COPY ["src/HeadendHQ.Data/HeadendHQ.Data.csproj", "src/HeadendHQ.Data/"]
COPY ["src/HeadendHQ.HdHomerun/HeadendHQ.HdHomerun.csproj", "src/HeadendHQ.HdHomerun/"]
COPY ["src/HeadendHQ.Nba/HeadendHQ.Nba.csproj", "src/HeadendHQ.Nba/"]
COPY ["src/HeadendHQ.AdbMapping/HeadendHQ.AdbMapping.csproj", "src/HeadendHQ.AdbMapping/"]
COPY ["src/HeadendHQ.DummyVideo/HeadendHQ.DummyVideo.csproj", "src/HeadendHQ.DummyVideo/"]

RUN dotnet restore "src/HeadendHQ.Web/HeadendHQ.Web.csproj"

COPY . .

WORKDIR "/src/src/HeadendHQ.Web"
RUN dotnet publish "HeadendHQ.Web.csproj" -c Release -o /app/publish --no-restore

# Download Playwright Chromium browser into a known path
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && PLAYWRIGHT_BROWSERS_PATH=/pw-browsers /root/.dotnet/tools/playwright install chromium

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Chromium system dependencies and ffmpeg (Ubuntu Noble / 24.04)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnspr4 libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 \
    libxrandr2 libgbm1 libasound2t64 libpango-1.0-0 libcairo2 \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /pw-browsers /pw-browsers
ENV PLAYWRIGHT_BROWSERS_PATH=/pw-browsers

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HeadendHQ.Web.dll"]
