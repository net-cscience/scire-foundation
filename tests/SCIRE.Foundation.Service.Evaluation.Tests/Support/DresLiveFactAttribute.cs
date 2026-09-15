using System;
using Xunit;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Support;

internal sealed class DresLiveFactAttribute : FactAttribute
{
    public DresLiveFactAttribute(bool force = false)
    {
        if (force)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_ENDPOINT")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_USERNAME")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DRES_PASSWORD")))
        {
            this.Skip = "Set DRES_ENDPOINT, DRES_USERNAME and DRES_PASSWORD to run against a live DRES instance.";
        }
    }
}