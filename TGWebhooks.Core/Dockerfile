FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY *.sln ./
COPY TGWebhooks.Core/TGWebhooks.Core.csproj TGWebhooks.Core/
COPY TGWebhooks.Interface/TGWebhooks.Interface.csproj TGWebhooks.Interface/

#ignore the nuget warnings about missing .Tests and the .dcproj

#TODO copy all plugins for nuget restore
COPY TGWebhooks.MaintainerApproval/TGWebhooks.MaintainerApproval.csproj TGWebhooks.MaintainerApproval/
COPY TGWebhooks.PRTagger/TGWebhooks.PRTagger.csproj TGWebhooks.PRTagger/
COPY TGWebhooks.TwentyFourHourRule/TGWebhooks.TwentyFourHourRule.csproj TGWebhooks.TwentyFourHourRule/
#END

RUN dotnet restore
COPY . .
WORKDIR /src/TGWebhooks.Core
RUN dotnet build -c Release -o /app

#TODO switch to each plugin and build it to /app/Plugins
WORKDIR /src/TGWebhooks.MaintainerApproval
RUN dotnet build -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.PRTagger
RUN dotnet build -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.TwentyFourHourRule
RUN dotnet build -c Release -o /app/Plugins
#END

FROM build AS publish
WORKDIR /src/TGWebhooks.Core
RUN dotnet publish -c Release -o /app

#TODO switch to each plugin and publish it to /app/Plugins
WORKDIR /src/TGWebhooks.MaintainerApproval
RUN dotnet publish -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.PRTagger
RUN dotnet publish -c Release -o /app/Plugins
WORKDIR /src/TGWebhooks.TwentyFourHourRule
RUN dotnet publish -c Release -o /app/Plugins
#END

WORKDIR /app/Plugins
RUN del "TGWebhooks.Interface.*"

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TGWebhooks.Core.dll"]
