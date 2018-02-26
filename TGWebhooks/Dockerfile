FROM microsoft/aspnetcore AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build AS build
WORKDIR /src
COPY . .

RUN dotnet restore TGWebhooks/TGWebhooks.csproj
WORKDIR /src/TGWebhooks
RUN dotnet build -c Release -o /app

FROM build AS publish
WORKDIR /src/TGWebhooks
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGWebhooks.dll"]
