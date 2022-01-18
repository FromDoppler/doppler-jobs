FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS restore
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

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS final
# We need these changes in openssl.cnf to access to our SQL Server instances in QA and INT environments
# See more information in https://stackoverflow.com/questions/56473656/cant-connect-to-sql-server-named-instance-from-asp-net-core-running-in-docker/59391426#59391426
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ARG version=unknown
RUN echo $version > /app/wwwroot/version.txt
ENTRYPOINT ["dotnet", "Doppler.Jobs.Server.dll"]