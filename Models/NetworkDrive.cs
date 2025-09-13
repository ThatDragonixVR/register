namespace MediaScanner.Models;

public class NetworkDrive
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DriveType Type { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime LastScan { get; set; }
    public int MediaCount { get; set; }
}

public enum DriveType
{
    NetworkShare,
    UsbDrive,
    LocalDrive,
    CloudDrive
}