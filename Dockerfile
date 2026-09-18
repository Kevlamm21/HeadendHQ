FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["HeadendHQ.Services/src/HeadendHQ.Web/HeadendHQ.Web.csproj", "HeadendHQ.Services/src/HeadendHQ.Web/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.AspNet/HeadendHQ.AspNet.csproj", "HeadendHQ.Services/src/HeadendHQ.AspNet/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.Core/HeadendHQ.Core.csproj", "HeadendHQ.Services/src/HeadendHQ.Core/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.Data/HeadendHQ.Data.csproj", "HeadendHQ.Services/src/HeadendHQ.Data/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.Mediator/HeadendHQ.Mediator.csproj", "HeadendHQ.Services/src/HeadendHQ.Mediator/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.HdHomerun/HeadendHQ.HdHomerun.csproj", "HeadendHQ.Services/src/HeadendHQ.HdHomerun/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.Nfo/HeadendHQ.Nfo.csproj", "HeadendHQ.Services/src/HeadendHQ.Nfo/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.SixLabors/HeadendHQ.SixLabors.csproj", "HeadendHQ.Services/src/HeadendHQ.SixLabors/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.FFmpeg/HeadendHQ.FFmpeg.csproj", "HeadendHQ.Services/src/HeadendHQ.FFmpeg/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.VodLauncher/HeadendHQ.VodLauncher.csproj", "HeadendHQ.Services/src/HeadendHQ.VodLauncher/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.Hangfire/HeadendHQ.Hangfire.csproj", "HeadendHQ.Services/src/HeadendHQ.Hangfire/"]
COPY ["HeadendHQ.Services/src/HeadendHQ.WebScraping/HeadendHQ.WebScraping.csproj", "HeadendHQ.Services/src/HeadendHQ.WebScraping/"]

RUN dotnet restore "HeadendHQ.Services/src/HeadendHQ.Web/HeadendHQ.Web.csproj"

COPY . .

WORKDIR "/src/HeadendHQ.Services/src/HeadendHQ.Web"
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
