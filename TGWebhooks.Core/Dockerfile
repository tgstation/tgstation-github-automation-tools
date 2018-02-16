FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY . .
RUN dotnet restore TGWebhooks.Core/TGWebhooks.Core.csproj
WORKDIR /src/TGWebhooks.Core
RUN dotnet build -c Release -o /app

FROM build AS publish
WORKDIR /src/TGWebhooks.Core
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGWebhooks.Core.dll"]
