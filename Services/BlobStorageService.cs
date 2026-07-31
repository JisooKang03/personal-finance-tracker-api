using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace PersonalFinanceTracker.Api.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureBlobStorage:ConnectionString"]!;
            var containerName = config["AzureBlobStorage:ContainerName"]!;

            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadReceiptAsync(IFormFile file, int userId, int transactionId)
        {
            // Unique blob name: userId/transactionId/timestamp-originalfilename
            // Prevents filename collisions and keeps receipts organized per user
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{userId}/{transactionId}/{DateTime.UtcNow.Ticks}{extension}";

            var blobClient = _containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteReceiptAsync(string receiptUrl)
        {
            var uri = new Uri(receiptUrl);
            var blobName = uri.AbsolutePath.Split('/', 3)[2]; // strip container name from path

            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public string GenerateReceiptSasUrl(string receiptUrl, int expiryMinutes = 15)
        {
            var uri = new Uri(receiptUrl);
            var blobName = uri.AbsolutePath.Split('/', 3)[2]; // strip container name from path

            var blobClient = _containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b", // "b" = blob (as opposed to "c" for container)
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }
}