using Azure.Storage.Blobs;
using Azure.Storage;
using AzurePractiseTask.ModelsDto;
using AzurePractiseTask.Models;
using AzurePractiseTask.Enums;
using Azure.Messaging.ServiceBus;
using System.Text;
using Newtonsoft.Json;


namespace AzurePractiseTask.Services
{
    public class AzureBlobService
    {
        private readonly string _storageAccount = "uploadphotostorage";
        private readonly string _accessKey = "/vZV9F4mKarHBiHZA4uG5dtmp2g8PpZngr9xwLguwHUh7nIqbyIijnMxv8PlvzMBwVBL33FFL4Y++ASt6CVdKA==";
        private readonly BlobContainerClient _filesContainer;
        private readonly CosmosDBService _cosmosDBService;
        private readonly string _serviceBusConnectionString;
        private readonly string _topicName;

        public AzureBlobService(CosmosDBService cosmosDBService)
        {
            var credential = new StorageSharedKeyCredential(_storageAccount, _accessKey);
            var blobUri = $"https://{_storageAccount}.blob.core.windows.net";
            var blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);
            _filesContainer = blobServiceClient.GetBlobContainerClient("files");
            _cosmosDBService = cosmosDBService;
            _serviceBusConnectionString = "Endpoint=sb://standartbusservicephoto.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=lN0bWjHvn6BAh4sHRVQSx12l7xgx+MlHG+ASbCY841I=";
            _topicName = "newphotoentity";
        }

        public async Task<List<BlobsDto>> ListAsync()
        {
            List<BlobsDto> files = new List<BlobsDto>();

            await foreach (var file in _filesContainer.GetBlobsAsync())
            {
                string uri = _filesContainer.Uri.ToString();
                var name = file.Name;
                var fullUri = $"{uri}/{name}";

                files.Add(new BlobsDto
                {
                    Uri = fullUri,
                    Name = name,
                    Type = file.Properties.ContentType
                });
            }

            return files;
        }

        public async Task<UploadFileResponseDto> UploadAsync(IFormFile blob, bool isProcessed)
        {
            UploadFileResponseDto responseDto = new UploadFileResponseDto();
            BlobClient client = _filesContainer.GetBlobClient(blob.FileName);

            Guid taskId = Guid.NewGuid();

            responseDto.id = taskId;

            await using (Stream? data = blob.OpenReadStream())
            {
                await client.UploadAsync(data);
            }

            if (!isProcessed) 
            {
                TaskStateModel taskState = new TaskStateModel
                {
                    id = taskId,
                    FileName = blob.FileName,
                    OriginalFilePath = $"https://uploadphotostorage.blob.core.windows.net/files/{blob.FileName}",
                    ProcessedFilePath = $"https://uploadphotostorage.blob.core.windows.net/files/processed_{blob.FileName}",
                    State = State.Created
                };

                string message = JsonConvert.SerializeObject(taskState);

                await using var busClient = new ServiceBusClient(_serviceBusConnectionString);
                var sender = busClient.CreateSender(_topicName);

                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message));
                await sender.SendMessageAsync(serviceBusMessage);

                var response = await _cosmosDBService.AddOrUpdateItemAsync(taskState);
            }
            
            return responseDto;
        }

        public async Task<BlobsDto?> DownloadAsync(string blobFilename)
        {
            BlobClient file = _filesContainer.GetBlobClient(blobFilename);

            if (await file.ExistsAsync())
            {
                var data = await file.OpenReadAsync();
                Stream blobContent = data;

                var content = await file.DownloadContentAsync();

                string name = blobFilename;
                string contentType = content.Value.Details.ContentType;

                return new BlobsDto { Content = blobContent, Name = name, Type = contentType };
            }

            return null;
        }

        public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
        {
            BlobClient file = _filesContainer.GetBlobClient(blobFilename);

            await file.DeleteAsync();

            return new BlobResponseDto { Error = false, Status = $"File: {blobFilename} has been successed delete" };
        }
    }
}
