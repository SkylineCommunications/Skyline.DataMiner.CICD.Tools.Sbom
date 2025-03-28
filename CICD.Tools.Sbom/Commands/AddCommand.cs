namespace Skyline.DataMiner.CICD.Tools.Sbom.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.Sbom.SystemCommandLine;

    internal class AddCommand : Command
    {
        public AddCommand() : base(name: "add", description: "Adds the specified SBOM to the DataMiner package.")
        {
            AddOption(option: new Option<FileInfo>(
                aliases: ["--sbom-file", "-s"],
                description: "The SBOM file path.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly()!.ExistingOnly());

            AddOption(option: new Option<FileInfo>(
                aliases: ["--package-file", "-p"],
                description: "The package file path to add the SBOM file to.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly()!.ExistingOnly());

            AddOption(option: new Option<DirectoryInfo?>(
                aliases: ["--output", "-o"],
                description: "The output directory to place the package with the SBOM file included.",
                parseArgument: OptionHelper.ParseDirectoryInfo)
            {
                IsRequired = false
            }.LegalFilePathsOnly());
        }
    }

    internal class AddCommandHandler(ILogger<AddCommandHandler> logger) : ICommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required FileInfo SbomFile { get; set; }

        public required FileInfo PackageFile { get; set; }

        public DirectoryInfo? Output { get; set; }

        public int Invoke(InvocationContext context)
        {
            throw new NotImplementedException();
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            logger.LogDebug($"### Starting {nameof(InvokeAsync)}");

            try
            {
                PackageHelper.AddSbomToPackage(PackageFile, SbomFile, Output);
                return Task.FromResult(0);
            }
            catch (NotSupportedException)
            {
                logger.LogError("The provided package type is not supported.");
                return Task.FromResult(1);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Exception during {nameof(InvokeAsync)}: {{e}}", e);
                return Task.FromResult(1);
            }
            finally
            {
                logger.LogDebug($"### Finishing {nameof(InvokeAsync)}");
            }
        }
    }
}