namespace AzurePractiseTask.ModelsDto
{
    public class BlobResponseDto
    {
        public BlobResponseDto() 
        {
            Blob = new BlobsDto();
        }

        public string? Status { get; set; }
        public bool Error { get; set; }
        public BlobsDto Blob { get; set; }
    }
}
