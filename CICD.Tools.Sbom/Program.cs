namespace Skyline.DataMiner.CICD.Tools.Sbom
{
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Hosting;
    using System.CommandLine.Parsing;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Sbom.Extensions.DependencyInjection;

    using Serilog.Events;

    using Skyline.DataMiner.CICD.Tools.Sbom.Commands;
    using Skyline.DataMiner.CICD.Tools.Sbom.Services;

    /// <summary>
    /// This .NET tool allows you to create a Software Bill of Materials (SBOM) for a given directory. It also allows you to add the SBOM to a package.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Code that will be called when running the tool.
        /// </summary>
        /// <param name="args">Extra arguments.</param>
        /// <returns>0 if successful.</returns>
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("This .NET tool allows you to create a Software Bill of Materials (SBOM) for a given directory. It also allows you to add the SBOM to a package.")
            {
                new AddCommand(),
                new GenerateCommand(),
                new GenerateAndAddCommand()
            };

            var isDebug = new Option<bool>(
                name: "--debug",
                description: "Indicates the tool should write out debug logging.")
            {
                IsRequired = false,
                IsHidden = true
            };

            var logLevel = new Option<LogEventLevel?>(
                name: "--minimum-log-level",
                description: "Indicates what the minimum log level should be. Default is Information");

            rootCommand.AddGlobalOption(isDebug);
            rootCommand.AddGlobalOption(logLevel);

            ParseResult parseResult = rootCommand.Parse(args);
            LogEventLevel level = parseResult.GetValueForOption(isDebug)
                ? LogEventLevel.Debug
                : parseResult.GetValueForOption(logLevel) ?? LogEventLevel.Information;

            var builder = new CommandLineBuilder(rootCommand).UseHost(host =>
            {
                host.ConfigureServices(services =>
                    {
                        services.AddSbomTool(level);
                        services.AddSingleton<ISbomService, SbomService>();
                    })
                    .UseCommandHandler<AddCommand, AddCommandHandler>()
                    .UseCommandHandler<GenerateCommand, GenerateCommandHandler>()
                    .UseCommandHandler<GenerateAndAddCommand, GenerateAndAddCommandHandler>();
            });

            return await builder.Build().InvokeAsync(args);
        }
    }
}