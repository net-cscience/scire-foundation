# Set DRES_ENDPOINT, DRES_USERNAME and DRES_PASSWORD in your local environment first.
# DRES_EVALUATION_ID optionally validates access to one particular run.
# DRES_TEST_VIEWER_STATE=1 enables the detailed-state test, which requires viewing permission.

$env:DRES_ENDPOINT = "http://10.34.64.205:8080"
$env:DRES_USERNAME = "user1"
$env:DRES_PASSWORD = "password1"
$env:DRES_EVALUATION_ID = "5415a4b5-e0ab-41dd-b5fb-841d3bfc0e70"
dotnet test --filter "Category=DresLive"
