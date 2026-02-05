namespace DocProcessor.Core.Enums;

public enum ProcessingMode
{
    RealTime = 0,
    Batch = 1
}

public enum ProcessingStatus
{
    Pending = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    BatchSubmitted = 5
}

public enum DocumentType
{
    Pdf = 0,
    Word = 1,
    Image = 2,
    Html = 3,
    Text = 4,
    Unknown = 99
}
