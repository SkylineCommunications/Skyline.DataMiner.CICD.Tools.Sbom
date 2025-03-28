namespace Skyline.DataMiner.CICD.Tools.Sbom.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Sbom.Contracts;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileSystemInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.Sbom.Services;
    using Skyline.DataMiner.CICD.Tools.Sbom.SystemCommandLine;

    internal class GenerateCommand : Command
    {
        public GenerateCommand() : base(name: "generate", description: "Generates a SBOM file for the provided directory.")
        {
            AddOption(option: new Option<IFileSystemInfoIO>(
                aliases: ["--solution-path", "-s"],
                description: "The directory containing the solution or the solution file itself",
                parseArgument: OptionHelper.ParseFileSystemInfo!)
            {
                IsRequired = true
            }!.ExistingOnly());

            AddOption(option: new Option<string?>(
                aliases: ["--package-name", "-pn"],
                description: "The name of the package the SBOM represents. Will default to the solution directory name.")
            {
                IsRequired = false
            });

            AddOption(option: new Option<string?>(
                aliases: ["--package-version", "-pv"],
                description: "The version of the package the SBOM represents.")
            {
                IsRequired = true
            });

            AddOption(option: new Option<string>(
                aliases: ["--package-supplier", "-ps"],
                description: "The supplier of the package the SBOM represents.")
            {
                IsRequired = true
            });

            AddOption(option: new Option<DirectoryInfo>(
                aliases: ["--output", "-o"],
                description: "The directory where the SBOM file will be placed.",
                parseArgument: OptionHelper.ParseDirectoryInfo!)
            {
                IsRequired = true
            });
        }
    }

    internal class GenerateCommandHandler(ISbomService sbomService, ILogger<GenerateCommandHandler> logger)
        : ICommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required IFileSystemInfoIO SolutionPath { get; set; }

        public string? PackageName { get; set; }

        public required string PackageVersion { get; set; }

        public required string PackageSupplier { get; set; }

        public required DirectoryInfo Output { get; set; }

        public int Invoke(InvocationContext context)
        {
            throw new NotImplementedException();
        }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var temporaryDirectory = new DirectoryInfo(FileSystem.Instance.Directory.CreateTemporaryDirectory());

            try
            {
                IDirectoryInfoIO solutionDirectory = SolutionPath switch
                {
                    IDirectoryInfoIO directory => directory,
                    IFileInfoIO file => new DirectoryInfo(FileSystem.Instance.Path.GetDirectoryName(file.FullName)),
                    _ => throw new InvalidOperationException($"{nameof(SolutionPath)} is not a directory or file.")
                };

                var metadata = new SBOMMetadata
                {
                    PackageName = PackageName ?? solutionDirectory.Name,
                    PackageSupplier = PackageSupplier,
                    PackageVersion = PackageVersion
                };

                var sbomFilePath = await sbomService.GenerateAsync(metadata, solutionDirectory, temporaryDirectory);
                if (sbomFilePath == null)
                {
                    logger.LogError("Failed to generate SBOM file.");
                    return 1;
                }

                Output.Create();
                var redactedSbomFilePath = await sbomService.RedactAsync(sbomFilePath, Output, context.GetCancellationToken());
                logger.LogInformation("SBOM file created at {redactedSbomFilePath}", redactedSbomFilePath);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Exception during {nameof(GenerateCommand)}: {{e}}", e);
                return 1;
            }
            finally
            {
                temporaryDirectory.Delete(true);
            }

            return 0;
        }
    }
}