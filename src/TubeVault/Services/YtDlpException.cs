namespace TubeVault;

internal sealed class YtDlpException : Exception
{
    public YtDlpException(
        YtDlpErrorKind kind,
        string message,
        int? exitCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        ExitCode = exitCode;
    }

    public YtDlpErrorKind Kind { get; }

    public int? ExitCode { get; }
}

internal enum YtDlpErrorKind
{
    Preparation,
    InvalidUrl,
    Unavailable,
    Network,
    General
}
