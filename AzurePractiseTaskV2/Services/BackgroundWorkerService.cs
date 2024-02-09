using Azure.Messaging.ServiceBus;
using AzurePractiseTask.Enums;
using AzurePractiseTask.Models;
using AzurePractiseTask.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


public class BackgroundWorkerService : BackgroundService
{
    private  readonly AzureBlobService _azureBlobService;
    private readonly CosmosDBService _cosmosDBService;

    public BackgroundWorkerService(AzureBlobService azureBlobService, CosmosDBService cosmosDBService)
    {
        _azureBlobService = azureBlobService;
        _cosmosDBService = cosmosDBService;
    }
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Debug.WriteLine("Programm is started");

        await using var client = new ServiceBusClient("Endpoint=sb://standartbusservicephoto.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=lN0bWjHvn6BAh4sHRVQSx12l7xgx+MlHG+ASbCY841I=");

        var receiver = client.CreateReceiver("newphotoentity", "tasksubscription");

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await receiver.ReceiveMessageAsync();

            if (message != null)
            {
                await receiver.CompleteMessageAsync(message);

                TaskStateModel taskState = JsonConvert.DeserializeObject<TaskStateModel>(message.Body.ToString());
                Debug.WriteLine(taskState.FileName);

                TaskStateUpdateModel updateData = new TaskStateUpdateModel
                {
                    id = taskState.id,
                    State = State.InProgress
                };

                var response = await _cosmosDBService.UpdateItemAsync(updateData);

                var result = await _azureBlobService.DownloadAsync(taskState.FileName);

                MemoryStream memoryStream = new MemoryStream();
                result.Content.CopyTo(memoryStream);
                memoryStream.Position = 0;

                FormFile file = new FormFile(memoryStream, 0, memoryStream.Length, null, $"processed_{taskState.FileName}");

                long fileSizeInBytes = file.Length;
                double fileSizeInKb = (double)fileSizeInBytes / 1024;

                if (fileSizeInKb > 200)
                {
                    updateData.State = State.Error;
                    var r = await _cosmosDBService.UpdateItemAsync(updateData);
                }
                else
                {
                    var rotatedStream = new MemoryStream();
                    await RotateImage(memoryStream, rotatedStream);
                    FormFile fileRotated = new FormFile(rotatedStream, 0, rotatedStream.Length, null, $"processed_{taskState.FileName}");
                    var resultS = await _azureBlobService.UploadAsync(fileRotated, true);
                    
                    updateData.State = State.Done;
                    var cosmos_response = await _cosmosDBService.UpdateItemAsync(updateData);
                }                
            }

            await Task.Delay(1000);
        }
    }

    private async Task RotateImage(Stream input, Stream output)
    {
        using (Image image = await Image.LoadAsync(input))
        {
            // Вращение изображения на 180 градусов
            image.Mutate(x => x.Rotate(RotateMode.Rotate180));

            // Сохранение изображения обратно в поток
            await image.SaveAsync(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());

            // Сброс позиции потока на начало
            output.Position = 0;
        }
    }
}