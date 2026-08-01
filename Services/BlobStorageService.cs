using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace PersonalFinanceTracker.Api.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _receiptsContainer;
        private readonly BlobContainerClient _profilePhotosContainer;

        public BlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureBlobStorage:ConnectionString"]!;
            var receiptsContainerName = config["AzureBlobStorage:ContainerName"]!;

            _receiptsContainer = new BlobContainerClient(connectionString, receiptsContainerName);
            _profilePhotosContainer = new BlobContainerClient(connectionString, "profile-photos");
        }

        public async Task<string> UploadReceiptAsync(IFormFile file, int userId, int transactionId)
        {
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{userId}/{transactionId}/{DateTime.UtcNow.Ticks}{extension}";

            var blobClient = _receiptsContainer.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteReceiptAsync(string receiptUrl)
        {
            var blobClient = GetBlobClientFromUrl(receiptUrl, _receiptsContainer);
            await blobClient.DeleteIfExistsAsync();
        }

        public string GenerateReceiptSasUrl(string receiptUrl, int expiryMinutes = 15)
        {
            return GenerateSasUrl(receiptUrl, _receiptsContainer, expiryMinutes);
        }

        public async Task<string> UploadProfilePhotoAsync(IFormFile file, int userId)
        {
            var extension = Path.GetExtension(file.FileName);
            // No transactionId needed here, just userId + timestamp, since a user has only one photo at a time
            var blobName = $"{userId}/{DateTime.UtcNow.Ticks}{extension}";

            var blobClient = _profilePhotosContainer.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteProfilePhotoAsync(string photoUrl)
        {
            var blobClient = GetBlobClientFromUrl(photoUrl, _profilePhotosContainer);
            await blobClient.DeleteIfExistsAsync();
        }

        public string GenerateProfilePhotoSasUrl(string photoUrl, int expiryMinutes = 60)
        {
            return GenerateSasUrl(photoUrl, _profilePhotosContainer, expiryMinutes);
        }

        // Shared helper: extracts the blob name from a full URL, resolved against a specific container
        private BlobClient GetBlobClientFromUrl(string blobUrl, BlobContainerClient container)
        {
            var uri = new Uri(blobUrl);
            var blobName = uri.AbsolutePath.Split('/', 3)[2];
            return container.GetBlobClient(blobName);
        }

        // Shared helper: generates a signed, time-limited read-only URL for any blob in a given container
        private string GenerateSasUrl(string blobUrl, BlobContainerClient container, int expiryMinutes)
        {
            var uri = new Uri(blobUrl);
            var blobName = uri.AbsolutePath.Split('/', 3)[2];

            var blobClient = container.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = container.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }
}