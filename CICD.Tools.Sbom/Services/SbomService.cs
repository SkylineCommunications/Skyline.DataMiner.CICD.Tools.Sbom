namespace Skyline.DataMiner.CICD.Tools.Sbom.Services
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Sbom.Api.FormatValidator;
    using Microsoft.Sbom.Api.Workflows.Helpers;
    using Microsoft.Sbom.Contracts;
    using Microsoft.Sbom.Parsers.Spdx22SbomParser.Entities;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;

    internal interface ISbomService
    {
        Task<IFileInfoIO?> GenerateAsync(SBOMMetadata metadata, IDirectoryInfoIO scanDirectory, IDirectoryInfoIO outputDirectory);

        Task<IFileInfoIO?> GenerateAsync(SBOMMetadata metadata, IDirectoryInfoIO rootDirectory,
            IDirectoryInfoIO componentDirectory, IDirectoryInfoIO outputDirectory);

        Task<IFileInfoIO?> RedactAsync(IFileInfoIO sbomPath, IDirectoryInfoIO outputDirectory, CancellationToken cancellationToken = default);
    }

    internal class SbomService(ISBOMGenerator generator, ISbomRedactor redactor, ILogger<SbomService> logger) : ISbomService
    {
        public async Task<IFileInfoIO?> GenerateAsync(SBOMMetadata metadata, IDirectoryInfoIO rootDirectory, IDirectoryInfoIO componentDirectory, IDirectoryInfoIO outputDirectory)
        {
            logger.LogDebug($"### Starting {nameof(GenerateAsync)}");

            try
            {
                outputDirectory.Create();

                var configuration = new RuntimeConfiguration
                {
                    // Mandatory
                    NamespaceUriBase = "https://sbom.skyline.be"
                };

                SbomGenerationResult result = await generator.GenerateSbomAsync(rootPath: rootDirectory.FullName,
                    componentPath: componentDirectory.FullName,
                    metadata: metadata,
                    runtimeConfiguration: configuration,
                    manifestDirPath: outputDirectory.FullName);

                if (!result.IsSuccessful)
                {
                    foreach (EntityError resultError in result.Errors)
                    {
                        logger.LogError(resultError.Details);
                    }

                    return null;
                }

                // Find file. There should be only one.
                var sbomFilePath = outputDirectory
                                   .GetFiles("*.spdx.json", System.IO.SearchOption.AllDirectories)
                                   .Single();

                logger.LogInformation("Found SBOM file at {sbomFilePath}", sbomFilePath);
                return sbomFilePath;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Exception during {nameof(GenerateAsync)}: {{e}}", e);
                return null;
            }
            finally
            {
                logger.LogDebug($"### Ending {nameof(GenerateAsync)}");
            }
        }

        public Task<IFileInfoIO?> GenerateAsync(SBOMMetadata metadata, IDirectoryInfoIO scanDirectory, IDirectoryInfoIO outputDirectory)
        {
            return GenerateAsync(metadata, scanDirectory, scanDirectory, outputDirectory);
        }

        public async Task<IFileInfoIO?> RedactAsync(IFileInfoIO sbomPath, IDirectoryInfoIO outputDirectory, CancellationToken cancellationToken = default)
        {
            logger.LogDebug($"### Starting {nameof(RedactAsync)}");

            try
            {
                using IValidatedSBOM validatedSbom = new ValidatedSBOMFactory().CreateValidatedSBOM(sbomPath.FullName);

                // Remove file references
                FormatEnforcedSPDX2 formatEnforcedSpdx2 = await redactor.RedactSBOMAsync(validatedSbom);

                outputDirectory.Create();
                string newFile = FileSystem.Instance.Path.Combine(outputDirectory.FullName, "sbom.json");
                await using System.IO.FileStream stream = FileSystem.Instance.File.Create(newFile);
                await JsonSerializer.SerializeAsync(stream, formatEnforcedSpdx2, cancellationToken: cancellationToken);

                return new FileInfo(newFile);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Exception during {nameof(RedactAsync)}: {{e}}", e);
                return null;
            }
            finally
            {
                logger.LogDebug($"### Ending {nameof(RedactAsync)}");
            }
        }
    }
}