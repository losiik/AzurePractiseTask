using AzurePractiseTask.Enums;


namespace AzurePractiseTask.Models
{
    public class TaskStateModel
    {
        public Guid? id { get; set; }
        public string? FileName { get; set; }
        public string? OriginalFilePath { get; set; }
        public string? ProcessedFilePath { get; set; }
        public State? State { get; set; }
    }
}
