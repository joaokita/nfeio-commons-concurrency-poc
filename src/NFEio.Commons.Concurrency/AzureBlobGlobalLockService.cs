using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NFEio.Commons.Concurrency;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;
using Azure;

namespace NFEio.Commons.Concurrency;
public class AzureBlobGlobalLockServiceOptions
{
    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }
}

public class AzureBlobGlobalLockService : IGlobalLockService
{
    private readonly ILogger<AzureBlobGlobalLockService> _logger;

    private readonly BlobContainerClient _blobContainerClient;

    private readonly BlobClient _blobServiceClient;


    public AzureBlobGlobalLockService(ILogger<AzureBlobGlobalLockService> logger,
        IOptions<AzureBlobGlobalLockServiceOptions> options)
    {
        _logger = logger;

        if (string.IsNullOrEmpty(options.Value.ConnectionString))
            throw new ArgumentException(nameof(options.Value.ConnectionString));

        if (string.IsNullOrEmpty(options.Value.ContainerName))
            throw new ArgumentException(nameof(AzureBlobGlobalLockServiceOptions.ContainerName));

        BlobServiceClient blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);

        _blobServiceClient = blobContainerClient.GetBlobClient(options.Value.ContainerName);
    }

    public IGlobalLock AcquireLock(string name, TimeSpan lockTime)
    {
        try
        {
            if (!_blobServiceClient.ExistsAsync().Result)
            {
                AppendBlobClient appendBlobClient = _blobContainerClient.GetAppendBlobClient(name);
                appendBlobClient.CreateAsync().Wait();
            }
        }
        catch
        {
            throw new Exception(string.Format("Lock could not be acquired. [{0}]", name));
        }

        for (var i = 1; i <= 30; i++)
        {
            try
            {
                BlobLeaseClient blobLeaseClient = _blobServiceClient.GetBlobLeaseClient();
                Response<BlobLease> lease = blobLeaseClient.AcquireAsync(lockTime).Result;

                return new AzureBlobGlobalLock(_blobServiceClient, lease.Value.LeaseId);
            }
            catch (AggregateException)
            {
                Task.Delay(500).Wait();
            }
        }

        throw new Exception(string.Format("Lock could not be acquired. [{0}]", name));
    }

    public async Task<bool> DeleteLock(string name)
    {
        try
        {
            await Task.Delay(10);

            BlobClient blobRef = _blobContainerClient.GetBlobClient(name);

            blobRef?.DeleteIfExistsAsync().Wait();
        }
        catch { return false; }

        return true;
    }
}

internal class AzureBlobGlobalLock : IGlobalLock
{
    private readonly BlobClient _client;
    private readonly string _lease;

    public AzureBlobGlobalLock(BlobClient client, string lease)
    {
        _client = client;
        _lease = lease;
    }

    public void Dispose()
    {
        BlobLeaseClient leaseClient = _client.GetBlobLeaseClient(_lease);
        leaseClient.ReleaseAsync().Wait();
    }
}