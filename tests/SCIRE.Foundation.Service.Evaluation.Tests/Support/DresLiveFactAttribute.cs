namespace SCIRE.Foundation.Service.Evaluation.Tests.Support;

internal sealed class DresLiveFactAttribute : FactAttribute
{
    public DresLiveFactAttribute(bool requireViewerState = false)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_ENDPOINT")) || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_USERNAME")) || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_PASSWORD")))
        {
            this.Skip = "Set DRES_ENDPOINT, DRES_USERNAME and DRES_PASSWORD to run against a live DRES instance.";
        }
        else if (requireViewerState && (Environment.GetEnvironmentVariable("DRES_TEST_VIEWER_STATE") != "1" || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_EVALUATION_ID"))))
        {
            this.Skip = "Set DRES_EVALUATION_ID and DRES_TEST_VIEWER_STATE=1 for a run that permits participant viewing.";
        }
    }
}
