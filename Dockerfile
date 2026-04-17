FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/HeadendHQ.Web/HeadendHQ.Web.csproj", "src/HeadendHQ.Web/"]
COPY ["src/HeadendHQ.Core/HeadendHQ.Core.csproj", "src/HeadendHQ.Core/"]
COPY ["src/HeadendHQ.Data/HeadendHQ.Data.csproj", "src/HeadendHQ.Data/"]
COPY ["src/HeadendHQ.HdHomerun/HeadendHQ.HdHomerun.csproj", "src/HeadendHQ.HdHomerun/"]

RUN dotnet restore "src/HeadendHQ.Web/HeadendHQ.Web.csproj"

COPY . .

WORKDIR "/src/src/HeadendHQ.Web"
RUN dotnet publish "HeadendHQ.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HeadendHQ.Web.dll"]
