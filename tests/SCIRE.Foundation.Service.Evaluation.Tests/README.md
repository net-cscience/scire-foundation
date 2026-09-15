# Evaluation package tests

Place this project at `tests/SCIRE.Foundation.Service.Evaluation.Tests/` beside the library at `src/SCIRE.Foundation.Service.Evaluation/`.

Run the offline suite from the repository root:

```text
dotnet test tests/SCIRE.Foundation.Service.Evaluation.Tests/SCIRE.Foundation.Service.Evaluation.Tests.csproj -c Release
```

Live tests skip unless `DRES_ENDPOINT`, `DRES_USERNAME`, and `DRES_PASSWORD` are set. `DRES_EVALUATION_ID` optionally selects a run. Set `DRES_TEST_VIEWER_STATE=1` to additionally exercise the detailed state endpoint for a run that permits participant viewing.

`RunDresLiveTest.ps1` reads those settings from the environment. No credentials or server defaults are embedded in the test project.

Protected-operation fixtures require the session query parameter, so a future missing-session regression cannot pass merely because the mocked server ignores authentication. The test transport does not emulate implicit cookie authentication.
