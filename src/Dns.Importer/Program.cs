namespace Dns.Importer
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Modules;
    using Serilog;

    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public static async Task Main(string[] args)
        {
            var ct = CancellationTokenSource.Token;

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                CancellationTokenSource.Cancel();
                eventArgs.Cancel = true;
            };

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
                Log.Debug(
                    eventArgs.Exception,
                    "FirstChanceException event raised in {AppDomain}.",
                    AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                Log.Fatal(
                    (Exception)eventArgs.ExceptionObject,
                    "Encountered a fatal exception, exiting program.");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.MachineName.ToLowerInvariant()}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddCommandLine(args ?? new string[0])
                .Build();

            var container = ConfigureServices(configuration);
            var logger = container.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting Dns.Importer");

            try
            {
                var runner = container.GetRequiredService<DnsImporter>();

                Console.WriteLine("Press ENTER to start import...");
                Console.ReadLine();
                await runner.StartAsync(ct);

                logger.LogInformation("Running... Press CTRL + C to exit.");
                ct.WaitHandle.WaitOne();
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Encountered a fatal exception, exiting program.");
                Log.CloseAndFlush();

                // Allow some time for flushing before shutdown.
                Thread.Sleep(1000);
                throw;
            }

            logger.LogInformation("Stopping...");
            ct.WaitHandle.Close();
        }

        private static IServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            var builder = new ContainerBuilder();

            builder.RegisterModule(new LoggingModule(configuration, services));

            var tempProvider = services.BuildServiceProvider();
            var loggerFactory = tempProvider.GetService<ILoggerFactory>();

            builder.RegisterInstance(configuration);

            builder.RegisterModule(new HttpModule(configuration, services, loggerFactory));

            builder
                .RegisterType<DomainFetcher>()
                .SingleInstance();

            builder
                .RegisterType<DnsImporter>()
                .SingleInstance();

            builder.Populate(services);

            return new AutofacServiceProvider(builder.Build());
        }
    }
}
