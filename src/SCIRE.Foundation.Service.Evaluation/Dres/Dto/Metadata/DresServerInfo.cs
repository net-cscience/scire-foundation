namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Metadata;

internal sealed record DresServerInfo(
    string Version,
    long StartTime,
    long Uptime
    );
