FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY Doppler.Jobs.sln ./
COPY ["Doppler.Database/Doppler.Database.csproj", "Doppler.Database/"]
COPY ["DopplerJobsServer/Doppler.Jobs.Server.csproj", "DopplerJobsServer/"]
COPY ["CrossCutting/CrossCutting.csproj", "CrossCutting/"]
COPY ["DopplerCurrencyJob/Doppler.Currency.Job.csproj", "DopplerCurrencyJob/"]
COPY ["DopplerBillingJob/Doppler.Billing.Job.csproj", "DopplerBillingJob/"]
COPY ["NotificationsJob/Doppler.Notifications.Job.csproj", "NotificationsJob/"]
COPY ["DopplerJobTest/Doppler.Jobs.Test.csproj", "DopplerJobTest/"]
RUN dotnet restore

FROM restore AS build
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS test
RUN dotnet test

FROM build AS publish
RUN dotnet publish "DopplerJobsServer/Doppler.Jobs.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ARG version=unknown
RUN echo $version > /app/wwwroot/version.txt
ENTRYPOINT ["dotnet", "Doppler.Jobs.Server.dll"]