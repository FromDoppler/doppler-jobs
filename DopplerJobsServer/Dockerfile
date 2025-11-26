
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0  AS build
WORKDIR /src
COPY ["DopplerJobsServer/Doppler.Jobs.Server.csproj", "DopplerJobsServer/"]
COPY ["DopplerBillingJob/Doppler.Billing.Job.csproj", "DopplerBillingJob/"]
COPY ["Doppler.Database/Doppler.Database.csproj", "Doppler.Database/"]
COPY ["CrossCutting/CrossCutting.csproj", "CrossCutting/"]
COPY ["DopplerCurrencyJob/Doppler.Currency.Job.csproj", "DopplerCurrencyJob/"]
COPY ["DopplerCancelAccountJob/Doppler.CancelAccountWithScheduleCancellation.Job.csproj", "DopplerCancelAccountJob/"]
COPY ["OfflineConversionsJob/OfflineConversionsJob.csproj", "OfflineConversionsJob/"]
RUN dotnet restore "DopplerJobsServer/Doppler.Jobs.Server.csproj"
COPY . .
WORKDIR "/src/DopplerJobsServer"
RUN dotnet build "Doppler.Jobs.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.Jobs.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG version=unknown
RUN echo $version > /app/wwwroot/version.txt
ENTRYPOINT ["dotnet", "Doppler.Jobs.Server.dll"]