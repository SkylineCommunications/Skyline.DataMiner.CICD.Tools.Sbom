namespace Skyline.DataMiner.CICD.Tools.Sbom
{
    using System;
    using System.IO.Compression;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;

    internal static class PackageHelper
    {
        public static void AddSbomToPackage(IFileInfoIO packageFile, IFileInfoIO sbomFile, IDirectoryInfoIO? output = null)
        {
            ArgumentNullException.ThrowIfNull(packageFile);
            ArgumentNullException.ThrowIfNull(sbomFile);

            IFileInfoIO file = packageFile;
            if (output != null)
            {
                output.Create();

                // Copy to new location
                file = packageFile.CopyTo(FileSystem.Instance.Path.Combine(output.FullName, packageFile.Name));
            }

            using ZipArchive zipArchive = ZipFile.Open(file.FullName, ZipArchiveMode.Update);
            zipArchive.CreateEntryFromFile(sbomFile.FullName, "sbom.json");
        }
    }
}