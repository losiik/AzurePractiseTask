using AzurePractiseTask.Enums;
using AzurePractiseTask.Models;
using Microsoft.Azure.Cosmos;


namespace AzurePractiseTask.Services
{
    public class CosmosDBService
    {
        private readonly string _cosmosDBAccountUri = "https://photoprocessdb.documents.azure.com:443/";
        private readonly string _cosmosDBAccountPrimaryKey = "Veqq4yKh0vfMhw3dsrfJGZS5e83uyRhp5tlXH90vUAY9u1hco8XnX7UiMCeocKVjl0HeKq7YvnuPACDbNRGVPA==";
        private readonly string _cosmosDbName = "photoDB";
        private readonly string _cosmosDbContainerName = "photos";
        private readonly Container _containerClient;


        public CosmosDBService() 
        {
            CosmosClient cosmosDbClient = new CosmosClient(_cosmosDBAccountUri, _cosmosDBAccountPrimaryKey);
            _containerClient = cosmosDbClient.GetContainer(_cosmosDbName, _cosmosDbContainerName);
        }

        public async Task<ItemResponse<TaskStateModel>> AddOrUpdateItemAsync(TaskStateModel taskState)
        {
            var response = await _containerClient.UpsertItemAsync<TaskStateModel>(
                    item: taskState,
                    partitionKey: new PartitionKey(taskState.id.ToString())
                );

            return response;
        }

        public async Task<ItemResponse<TaskStateModel>> UpdateItemAsync(TaskStateUpdateModel updateData)
        {
            var item = await ReadNoteAsync(updateData.id.ToString());

            TaskStateModel model = new TaskStateModel 
            {
                id = updateData.id,
                FileName = item.Resource.FileName,
                OriginalFilePath = item.Resource.OriginalFilePath,
                ProcessedFilePath = item.Resource.ProcessedFilePath,
                State = updateData.State
            };

            var response = await AddOrUpdateItemAsync(model);

            return response;
        }
        public async Task<TaskStatusModel> ItemStatusAsync(string itemId) 
        {
            var item = await ReadNoteAsync(itemId);

            TaskStatusModel taskStatusModel = new TaskStatusModel
            {
                State = item.Resource.State.ToString()
            };

            if (item.Resource.State == State.Done)
            {
                taskStatusModel.FileUrl = item.Resource.ProcessedFilePath;
            }

            return taskStatusModel;
        }

        public async Task<ItemResponse<TaskStateModel>> ReadNoteAsync(string itemId)
        {
            var response = await _containerClient.ReadItemAsync<TaskStateModel>(
                    id: itemId,
                    partitionKey: new PartitionKey(itemId)
                );

            return response;
        }
    }
}
