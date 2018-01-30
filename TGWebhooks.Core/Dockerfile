FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY *.sln ./
COPY TGWebhooks.Core/TGWebhooks.Core.csproj TGWebhooks.Core/
RUN dotnet restore
COPY . .
WORKDIR /src/TGWebhooks.Core
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGWebhooks.Core.dll"]
