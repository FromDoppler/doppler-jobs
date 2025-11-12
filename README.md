# Doppler Jobs

This repository contains the background processes that support Doppler's platform. The solution hosts an ASP.NET Core server with Hangfire to schedule recurring tasks that interact with internal services (SAP, Billing User, OnSite, Beplic) and third-party systems.

## Architecture overview

The `Doppler.Jobs.sln` solution groups shared libraries, business-specific projects, and the `DopplerJobsServer` web host, which starts Hangfire and registers the jobs. The server exposes a dashboard at `/hangfire` (with a configurable prefix) and uses time zones and CRON expressions defined in the configuration for each job.

## Solution projects

```
├── CrossCutting/                Shared infrastructure and services (HTTP, email, date helpers, etc.).
├── DopplerJobsServer/           ASP.NET Core application that hosts Hangfire and registers the jobs.
├── DopplerBillingJob/           Synchronises billing data with SAP (global and US flows).
├── DopplerCurrencyJob/          Updates currency exchange rates and stores them in Doppler DB.
├── NotificationsJob/            Sends free-trial notification emails.
├── SurplusAddOnJob/             Reconciles OnSite and Conversations add-on consumption.
├── DopplerCancelAccountJob/     Cancels accounts on the scheduled date.
├── DopplerJobTest/              Unit and integration tests.
├── Dockerfile                   Multi-stage pipeline (restore, build, test, publish).
└── build-n-publish.sh           Build and publish script used by CI.
```

## Available jobs

All jobs are registered in `Startup.ConfigureJobsScheduler`, which uses `RecurringJob.AddOrUpdate` to associate the CRON expression and configured identifier. The default time zone is `Argentina Standard Time`.

| Job                                               | Description                                                                                                             |
| ------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `DopplerBillingJob`                               | Executes billing stored procedures (AR/legacy US) and sends the outcome to Doppler's SAP service.                       |
| `DopplerBillingUsJob`                             | Orchestrates the US billing flow, running QuickBooks procedures before synchronising with SAP.                          |
| `DopplerCurrencyJob`                              | Fetches daily exchange rates, publishes them to SAP, stores the values in the database, and reports errors to Hangfire. |
| `DopplerFreeTrialFinishesIn7DaysNotificationJob`  | Sends emails to users whose free trial ends in seven days using localised templates.                                    |
| `DopplerFreeTrialFinishesTodayNotificationJob`    | Notifies users on the last day of the free trial with the corresponding template.                                       |
| `DopplerFreeTrialExpiredNotificationJob`          | Reports the free-trial expiration using the configured templates.                                                       |
| `DopplerSurplusOnSiteJob`                         | Queries OnSite metrics, compares them with the contracted quota, and generates surplus charges when needed.             |
| `DopplerSurplusConversationsJob`                  | Applies the same surplus logic for conversations managed through the Beplic integration.                                |
| `DopplerCancelAccountWithScheduleCancellationJob` | Cancels accounts whose scheduled date has been reached and removes the flag once Billing User confirms the operation.   |

## Configuration

CRON expressions, job identifiers, the default time zone, and external service parameters are defined in the `DopplerJobsServer/appsettings*.json` files. The configuration supports overrides through `appsettings.Secret.json`, environment variables, or Docker secrets managed from `Program.cs`. Pay special attention to:

- `Jobs:*`: CRON schedule and Hangfire display name per job.
- `DopplerDatabaseSettings`: SQL Server connection string (the password is injected externally).
- SAP, exchange rate, PopUp Hub, Beplic, Billing User, and Relay sections.

## Local execution

1. Restore dependencies, build, and run the tests:

   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

   The `Dockerfile` reproduces these stages in a multi-stage pipeline before publishing the final image.

2. Start the Hangfire server:

   ```bash
   dotnet run --project DopplerJobsServer
   ```

   The dashboard will be available at `http://localhost:5000/hangfire` (plus the prefix configured via `PrefixHangfireDashboard`).

## Add a new job

1. Create the job class inside an existing or new project and apply `[AutomaticRetry]` with a policy aligned with the other jobs.
2. Register dependencies in `ConfigureServices`, including HTTP clients and typed configuration (`IOptions<T>`).
3. Add entries under `Jobs` in `appsettings.json` with the identifier, CRON expression, and specific configuration required by the job.
4. Schedule the job in `ConfigureJobsScheduler` using `RecurringJob.AddOrUpdate` and the previous configuration.
5. If you create a new project, include it in `Doppler.Jobs.sln` and update the restore stage in the `Dockerfile`.
6. Add tests in `DopplerJobTest` following the existing patterns (fixtures, fakes, etc.).

## Continuous integration and deployments

The Jenkins pipeline defined in `doppler-jenkins-ci.groovy` runs restore, build, test, and publish using the `build-n-publish.sh` script. Depending on the branch or tag, it publishes images tagged as `pr-*`, `master`, `INT`, or semantic versions to Docker Hub (`dopplerdock/doppler-jobs`). The script generates consistent tags and pushes the required variants for production and testing environments.

To deploy:

1. Retrieve the image produced by the pipeline (according to the target environment).
2. Provide the required configuration through environment variables, secret files, or mounts.
3. Start the container, which executes `Doppler.Jobs.Server.dll` and runs the scheduled jobs.

## Automated tests

The `DopplerJobTest` project gathers unit and integration tests that cover SAP integrations, external services, email delivery, and JWT generation. Run the tests with `dotnet test` locally or via the `test` stage of the Dockerfile before publishing images.
