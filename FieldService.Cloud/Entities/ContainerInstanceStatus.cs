namespace FieldService.Cloud.Entities;

public enum ContainerInstanceStatus
{
    Provisioning = 1,
    Running = 2,
    Failed = 3,
    Stopped = 4,
    Unknown = 5,
    Deleting = 6,
    Canceled = 7
}
