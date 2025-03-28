namespace Skyline.DataMiner.CICD.Tools.Sbom.Commands
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO.Compression;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Sbom.Contracts;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileSystemInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.Sbom.Services;
    using Skyline.DataMiner.CICD.Tools.Sbom.SystemCommandLine;

    internal class GenerateAndAddCommand : Command
    {
        public GenerateAndAddCommand() : base(name: "generate-and-add",
            description: "Generates a SBOM file for the provided directory and adds it to the provided package.")
        {
            AddOption(option: new Option<IFileSystemInfoIO>(
                aliases: ["--solution-path", "-s"],
                description: "The directory containing the solution or the solution file itself",
                parseArgument: OptionHelper.ParseFileSystemInfo!)
            {
                IsRequired = true
            }!.ExistingOnly());

            AddOption(option: new Option<FileInfo>(
                aliases: ["--package-file", "-p"],
                description: "The package file path to add the SBOM file to.",
                parseArgument: OptionHelper.ParseFileInfo!)
            {
                IsRequired = true
            }.LegalFilePathsOnly()!.ExistingOnly());

            AddOption(option: new Option<string>(
                aliases: ["--package-name", "-pn"],
                description: "The name of the package the SBOM represents.")
            {
                IsRequired = true
            });

            AddOption(option: new Option<string>(
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

            AddOption(option: new Option<DirectoryInfo?>(
                aliases: ["--output", "-o"],
                description: "The output directory to place the package with the SBOM file included.",
                parseArgument: OptionHelper.ParseDirectoryInfo)
            {
                IsRequired = false
            }.LegalFilePathsOnly());
        }
    }

    internal class GenerateAndAddCommandHandler(ISbomService sbomService, ILogger<GenerateAndAddCommandHandler> logger) : ICommandHandler
    {
        /* Automatic binding with System.CommandLine.NamingConventionBinder */

        public required IFileSystemInfoIO SolutionPath { get; set; }

        public required FileInfo PackageFile { get; set; }

        public required string PackageName { get; set; }

        public required string PackageVersion { get; set; }

        public required string PackageSupplier { get; set; }

        public DirectoryInfo? Output { get; set; }

        public int Invoke(InvocationContext context)
        {
            throw new NotImplementedException();
        }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var temporaryDirectory = new DirectoryInfo(FileSystem.Instance.Directory.CreateTemporaryDirectory());

            try
            {
                IDirectoryInfoIO unzippedPackage = temporaryDirectory.CreateSubdirectory("Package");

                ZipFile.ExtractToDirectory(PackageFile.FullName, unzippedPackage.FullName);

                var metadata = new SBOMMetadata
                {
                    // Mandatory
                    PackageName = PackageName,
                    PackageSupplier = PackageSupplier,
                    PackageVersion = PackageVersion,
                };

                IDirectoryInfoIO solutionDirectory = SolutionPath switch
                {
                    IDirectoryInfoIO directory => directory,
                    IFileInfoIO file => new DirectoryInfo(FileSystem.Instance.Path.GetDirectoryName(file.FullName)),
                    _ => throw new InvalidOperationException($"{nameof(SolutionPath)} is not a directory or file.")
                };

                // Pass along the unzipped package to include the files in the SBOM as well.
                var sbomFilePath = await sbomService.GenerateAsync(metadata, unzippedPackage, solutionDirectory, temporaryDirectory);
                if (sbomFilePath == null)
                {
                    logger.LogError("Failed to generate SBOM file.");
                    return 1;
                }

                PackageHelper.AddSbomToPackage(PackageFile, sbomFilePath, Output);
                return 0;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Exception during {nameof(GenerateAndAddCommand)}: {{e}}", e);
                return 1;
            }
            finally
            {
                temporaryDirectory.Delete(true);
            }
        }
    }
}