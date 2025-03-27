namespace CICD.Tools.SbomTests
{
    using FluentAssertions;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;
    using Skyline.DataMiner.CICD.Tools.Sbom;

    [TestClass, TestCategory("IntegrationTest")]
    public class PackageHelperTests
    {
        private DirectoryInfo temporaryDirectory = null!;

        [TestInitialize]
        public void Initialize()
        {
            temporaryDirectory = new DirectoryInfo(FileSystem.Instance.Directory.CreateTemporaryDirectory());
        }

        [TestCleanup]
        public void Cleanup()
        {
            temporaryDirectory.Delete(true);
        }

        [TestMethod]
        public void AddSbomToPackageTest_AddFileToPackage()
        {
            // Arrange
            // Move package to temp folder as we'll modify the original package
            string originalPackage = FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "Packages", "SbomTest.1.0.0.dmapp");
            string packageFilePath = FileSystem.Instance.Path.Combine(temporaryDirectory.FullName, "SbomTest.1.0.0.dmapp");
            FileSystem.Instance.File.Copy(originalPackage, packageFilePath);

            FileInfo packageFile = new FileInfo(packageFilePath);
            FileInfo sbomFile = new FileInfo(FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomFiles", "RandomSbomFile.json"));

            long initialSize = packageFile.Length;

            // Act
            PackageHelper.AddSbomToPackage(packageFile, sbomFile);

            // Assert
            packageFile.Refresh();
            packageFile.Length.Should().BeGreaterThan(initialSize);
        }

        [TestMethod]
        public void AddSbomToPackageTest_AddFileToPackageWithOutput()
        {
            // Arrange
            FileInfo packageFile = new FileInfo(FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "Packages", "SbomTest.1.0.0.dmapp"));
            FileInfo sbomFile = new FileInfo(FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomFiles", "RandomSbomFile.json"));

            long initialSize = packageFile.Length;

            // Act
            PackageHelper.AddSbomToPackage(packageFile, sbomFile, temporaryDirectory);

            // Assert
            // Check original package
            packageFile.Refresh();
            packageFile.Length.Should().Be(initialSize, because: "the package should not be modified.");

            // Check new package
            temporaryDirectory.Refresh();
            temporaryDirectory.GetFiles().Should().ContainSingle()
                              .Which.Length.Should().BeGreaterThan(initialSize, because: "the package have the added sbom file.");
        }
    }
}