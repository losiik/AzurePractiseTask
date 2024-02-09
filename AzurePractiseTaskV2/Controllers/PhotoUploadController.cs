using AzurePractiseTask.Models;
using AzurePractiseTask.Services;
using Microsoft.AspNetCore.Mvc;


namespace AzurePractiseTask.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PhotoUploadController : ControllerBase
    {
        private readonly AzureBlobService _azureBlobService;
        private readonly CosmosDBService _cosmosDBService;

        public PhotoUploadController(AzureBlobService azureBlobService, CosmosDBService cosmosDBService, IConfiguration configuration)
        {
            _azureBlobService = azureBlobService;
            _cosmosDBService = cosmosDBService;
        }

        [HttpGet("task-state")]
        [ProducesResponseType(typeof(TaskStateModel), 200)]
        public async Task<IActionResult> TaskStateAsync(Guid taskId)
        {
            var response = await _cosmosDBService.ItemStatusAsync(taskId.ToString());

            return Ok(response);
        }

        [HttpPost("upload-img")]
        public async Task<IActionResult> UploadImgAsync(IFormFile file)
        {
            var result = await _azureBlobService.UploadAsync(file, false);
            return Ok(result);
        }
    }
}
