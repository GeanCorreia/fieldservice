namespace FieldService.Storage.Exceptions;


public class FileSizeExceededException : Exception
{
    public long ActualSizeInBytes { get; }
    public long MaxSizeInBytes { get; }

    public FileSizeExceededException(long actualSizeInBytes, long maxSizeInBytes)
        : base($"File size ({actualSizeInBytes} bytes) exceeds the maximum allowed limit of {maxSizeInBytes} bytes.")
    {
        ActualSizeInBytes = actualSizeInBytes;
        MaxSizeInBytes = maxSizeInBytes;
    }
}

public class InvalidContentTypeException : Exception
{
    public string ContentType { get; }
    public IReadOnlyCollection<string> AllowedContentTypes { get; }

    public InvalidContentTypeException(string contentType, IEnumerable<string> allowedContentTypes)
        : base($"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", allowedContentTypes)}.")
    {
        ContentType = contentType;
        AllowedContentTypes = allowedContentTypes.ToList().AsReadOnly();
    }
}

public class StorageTransactionRequiredException : Exception
{
    public string OperationName { get; }

    public StorageTransactionRequiredException(string operationName)
        : base($"The storage operation '{operationName}' requires an active database transaction to guarantee consistency.")
    {
        OperationName = operationName;
    }

    public StorageTransactionRequiredException()
        : base("An active database transaction is required to perform this storage operation.")
    {
        OperationName = "Unknown";
    }
}