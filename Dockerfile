FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/HeadendHQ.Web/HeadendHQ.Web.csproj", "src/HeadendHQ.Web/"]
RUN dotnet restore "src/HeadendHQ.Web/HeadendHQ.Web.csproj"
COPY . .
WORKDIR "/src/src/HeadendHQ.Web"
RUN dotnet publish "HeadendHQ.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HeadendHQ.Web.dll"]
