using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
            ConnectionString = null,
            ContainerName = null
        });

        var blob = BlobsModelFactory.BlobItem("mocked.mocked.mocked.mocked");

        var mock = GetBlobContainerClientMock();
        _service = new AzureBlobGlobalLockService(_loggerMock.Object, mock, _optionsMock.Object);
    }

    [TestMethod]
    public void AcquireLock_ShouldReturnException_WhenBlobDoesNotExist()
    {
        // Arrange
        string lockName = "testLock";
        TimeSpan lockTime = TimeSpan.FromMinutes(1);

        // Assert
        Assert.ThrowsException<Exception>(() => _service.AcquireLock(lockName, lockTime));
    }

    private static BlobContainerClient GetBlobContainerClientMock()
    {
        var mock = new Mock<BlobContainerClient>();
        
        mock
            .Setup(i => i.AccountName)
            .Returns("Test account name");

        mock.Setup(i => i.GetBlobClient(It.IsAny<string>()))
            .Returns(new Mock<BlobClient>().Object);

        return mock.Object;
    }
}