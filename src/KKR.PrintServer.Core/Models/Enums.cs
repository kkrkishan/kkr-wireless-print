namespace KKR.PrintServer.Core.Models;

public enum PrintDocumentType
{
    Unknown = 0,
    Pdf = 1,
    Jpeg = 2,
    Png = 3
}

public enum PrintOrientation
{
    Auto = 0,
    Portrait = 1,
    Landscape = 2
}

public enum PrintColorMode
{
    Auto = 0,
    Color = 1,
    Grayscale = 2
}

public enum PrintScalingMode
{
    FitPage = 0,
    FillPage = 1,
    ActualSize = 2
}

public enum JobStatus
{
    Accepted = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
