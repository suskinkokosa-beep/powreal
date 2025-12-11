FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PowerRealms.Api.csproj ./
RUN dotnet restore PowerRealms.Api.csproj
COPY . ./
RUN dotnet publish PowerRealms.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS="http://+:5000;https://+:5001"
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "PowerRealms.Api.dll"]
