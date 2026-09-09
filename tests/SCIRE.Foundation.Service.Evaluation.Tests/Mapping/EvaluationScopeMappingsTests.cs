using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Mapping;

public sealed class EvaluationScopeMappingsTests
{
    [Fact]
    public void Resolve_UsesRegisteredMapping()
    {
        var mappings = new EvaluationScopeMappings()
            .Map<TestResult, ItemSubmissionScope>(result =>
                new ItemSubmissionScope(result.VideoId));

        var result = mappings.Resolve(new TestResult("v_19576"));

        var item = Assert.IsType<ItemSubmissionScope>(result);
        Assert.Equal("v_19576", item.MediaItemName);
    }

    [Fact]
    public void Resolve_UsesAssignableMapping()
    {
        var mappings = new EvaluationScopeMappings()
            .Map<ITestResult, ItemSubmissionScope>(result =>
                new ItemSubmissionScope(result.VideoId));

        ITestResult result = new TestResult("v_19576");

        var scope = mappings.Resolve(result);

        Assert.Equal("v_19576", Assert.IsType<ItemSubmissionScope>(scope).MediaItemName);
    }


    private interface ITestResult
    {
        string VideoId { get; }
    }


    private sealed record TestResult(string VideoId) : ITestResult;

}