using Azure;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace NFEio.Commons.Concurrency.Tests;

[TestClass]
public class AzureBlobGlobalLockServiceTests
{
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly AzureBlobGlobalLockService _azureBlobGlobalLockService;

    private Mock<ILogger<AzureBlobGlobalLockService>> _loggerMock;
    private Mock<IOptions<AzureBlobGlobalLockServiceOptions>> _optionsMock;
    private AzureBlobGlobalLockService _service;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<AzureBlobGlobalLockService>>();
        _optionsMock = new Mock<IOptions<AzureBlobGlobalLockServiceOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(new AzureBlobGlobalLockServiceOptions
        {
            ConnectionString = "YourConnectionString",
            ContainerName = "YourContainerName"
        });

        _service = new AzureBlobGlobalLockService(_loggerMock.Object, _optionsMock.Object);
    }

    [TestMethod]
    public void AcquireLock_ShouldCreateBlob_WhenBlobDoesNotExist()
    {
        // Arrange
        string lockName = "testLock";
        TimeSpan lockTime = TimeSpan.FromMinutes(1);

        // Act
        var globalLock = _service.AcquireLock(lockName, lockTime);

        // Assert
        Assert.IsNotNull(globalLock);
    }
}