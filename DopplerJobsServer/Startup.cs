using CrossCutting;
using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Settings;
using Doppler.Billing.Job;
using Doppler.Billing.Job.Database;
using Doppler.Billing.Job.Settings;
using Doppler.Currency.Job;
using Doppler.Currency.Job.DopplerCurrencyService;
using Doppler.Currency.Job.Settings;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using CrossCutting.Authorization;
using Doppler.Database;
using Doppler.Notifications.Job;
using CrossCutting.EmailSenderService;
using Flurl.Http.Configuration;

namespace Doppler.Jobs.Server
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x =>
            {
                x.UseSQLiteStorage();
            });

            var httpClientPolicies = new HttpClientPoliciesSettings();
            Configuration.GetSection("HttpClient:Client").Bind(httpClientPolicies);
            services.AddSingleton(httpClientPolicies);

            var handlerHttpClient = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                SslProtocols = SslProtocols.Tls12
            };

            services.AddHttpClient(httpClientPolicies.ClientName, c => { })
                .ConfigurePrimaryHttpMessageHandler(() => handlerHttpClient)
                .AddTransientHttpErrorPolicy(builder => GetRetryPolicy(httpClientPolicies.Policies.RetryAttemps));

            services.Configure<DopplerCurrencyServiceSettings>(Configuration.GetSection(nameof(DopplerCurrencyServiceSettings)));

            var jobsConfig = new TimeZoneJobConfigurations
            {
                TimeZoneJobs = TimeZoneHelper.GetTimeZoneByOperativeSystem(Configuration["TimeZoneJobs"])
            };
            services.AddSingleton(jobsConfig);
            services.AddTransient<IDopplerCurrencyService, DopplerCurrencyService>();

            services.Configure<DopplerSapConfiguration>(Configuration.GetSection(nameof(DopplerSapConfiguration)));
            services.AddTransient<IDopplerSapService, DopplerSapService>();

            services.AddTransient<IDbConnectionFactory, DbConnectionFactory>();
            services.Configure<DopplerBillingJobSettings>(Configuration.GetSection("Jobs:DopplerBillingJobSettings"));
            services.Configure<DopplerBillingUsJobSettings>(Configuration.GetSection("Jobs:DopplerBillingUsJobSettings"));
            services.AddTransient<IDopplerRepository, DopplerRepository>();

            services.AddTransient<Notifications.Job.Database.IDopplerRepository, Notifications.Job.Database.DopplerRepository>();

            ConfigureJobsScheduler();

            services.AddTransient<JwtSecurityTokenHandler>();
            services.Configure<JwtOptions>(Configuration.GetSection(nameof(JwtOptions)));
            services.AddJwtToken();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            services.Configure<EmailNotificationsConfiguration>(Configuration.GetSection(nameof(EmailNotificationsConfiguration)));
            services.Configure<RelayEmailSenderSettings>(Configuration.GetSection(nameof(RelayEmailSenderSettings)));

            services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
            services.AddTransient<IEmailSender, RelayEmailSender>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                PrefixPath = Configuration["PrefixHangfireDashboard"],
                Authorization = new[] {new HangfireAuthorizationFilter()}
            });

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1
            });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retry)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying =
            {
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private void ConfigureJobsScheduler()
        {
            JobStorage.Current = new SQLiteStorage("Hangfire.db");
            
            var tz = TimeZoneHelper.GetTimeZoneByOperativeSystem(Configuration["TimeZoneJobs"]);

            RecurringJob.AddOrUpdate<DopplerBillingJob>(
                Configuration["Jobs:DopplerBillingJobSettings:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerBillingJobSettings:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));

            RecurringJob.AddOrUpdate<DopplerCurrencyJob>(
                Configuration["Jobs:DopplerCurrencyJob:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerCurrencyJob:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));

            RecurringJob.AddOrUpdate<DopplerBillingUsJob>(
                Configuration["Jobs:DopplerBillingUsJobSettings:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerBillingUsJobSettings:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));

            RecurringJob.AddOrUpdate<DopplerFreeTrialFinishesIn7DaysNotificationJob>(
                Configuration["Jobs:DopplerFreeTrialFinishesIn7DaysNotificationJobSettings:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerFreeTrialFinishesIn7DaysNotificationJobSettings:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));

            RecurringJob.AddOrUpdate<DopplerFreeTrialFinishesTodayNotificationJob>(
                Configuration["Jobs:DopplerFreeTrialFinishesTodayNotificationJobSettings:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerFreeTrialFinishesTodayNotificationJobSettings:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));

            RecurringJob.AddOrUpdate<DopplerFreeTrialExpiredNotificationJob>(
                Configuration["Jobs:DopplerFreeTrialExpiredNotificationJobSettings:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerFreeTrialExpiredNotificationJobSettings:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(tz));
        }
    }
}
