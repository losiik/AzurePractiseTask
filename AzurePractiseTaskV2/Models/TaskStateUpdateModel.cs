using AzurePractiseTask.Enums;

namespace AzurePractiseTask.Models
{
    public class TaskStateUpdateModel
    {
        public Guid? id { get; set; }
        public State? State { get; set; }
    }
}
