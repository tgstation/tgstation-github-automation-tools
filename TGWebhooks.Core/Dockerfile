FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY *.sln ./
COPY TGWebhooks.Core/TGWebhooks.Core.csproj TGWebhooks.Core/
COPY TGWebhooks.Api/TGWebhooks.Api.csproj TGWebhooks.Api/

#ignore the nuget warnings about missing .Tests and the .dcproj

#TODO copy all plugins for nuget restore
COPY TGWebhooks.Plugins/TGWebhooks.Plugins.MaintainerApproval/TGWebhooks.Plugins.MaintainerApproval.csproj TGWebhooks.Plugins/TGWebhooks.Plugins.MaintainerApproval/
COPY TGWebhooks.Plugins/TGWebhooks.Plugins.PRTagger/TGWebhooks.Plugins.PRTagger.csproj TGWebhooks.Plugins/TGWebhooks.Plugins.PRTagger/
COPY TGWebhooks.Plugins/TGWebhooks.Plugins.SignOff/TGWebhooks.Plugins.SignOff.csproj TGWebhooks.Plugins/TGWebhooks.Plugins.SignOff/
COPY TGWebhooks.Plugins/TGWebhooks.Plugins.TwentyFourHourRule/TGWebhooks.Plugins.TwentyFourHourRule.csproj TGWebhooks.Plugins/TGWebhooks.Plugins.TwentyFourHourRule/
#END

RUN dotnet restore
COPY . .
WORKDIR /src/TGWebhooks.Core
RUN dotnet build -c Release -o /app

#TODO switch to each plugin and build it to /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.MaintainerApproval
RUN dotnet build -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.PRTagger
RUN dotnet build -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.SignOff
RUN dotnet build -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.TwentyFourHourRule
RUN dotnet build -c Release -o /app/Plugins
#END

FROM build AS publish
WORKDIR /src/TGWebhooks.Core
RUN dotnet publish -c Release -o /app

#TODO switch to each plugin and publish it to /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.MaintainerApproval
RUN dotnet publish -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.PRTagger
RUN dotnet publish -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.SignOff
RUN dotnet publish -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.Plugins/TGWebhooks.Plugins.TwentyFourHourRule
RUN dotnet publish -c Release -o /app/Plugins
#END

WORKDIR /app/Plugins
RUN del "TGWebhooks.Api.*"

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGWebhooks.Core.dll"]
