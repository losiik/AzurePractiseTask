namespace AzurePractiseTask.ModelsDto
{
    public class BlobsDto
    {
        public string? Uri { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public Stream? Content { get; set; }
    }
}
