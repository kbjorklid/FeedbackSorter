
$solutionName = "FeedbackSorter"
$solutionDir = "." # Current directory
$srcDir = "src"
$testsDir = "tests"

# Create the test C# projects
$testProjects = @(
    "FeedbackSorter.Core.UnitTests",
    "FeedbackSorter.Application.UnitTests",
    "FeedbackSorter.Infrastructure.UnitTests",
    "FeedbackSorter.Presentation.UnitTests"
)

Write-Host "Creating test C# projects:"
foreach ($testProjectName in $testProjects) {
    Write-Host "Creating test project: $testProjectName"
    dotnet new xunit -n $testProjectName -o "$solutionDir/$testsDir/$testProjectName"
    dotnet sln $solutionDir/$solutionName.sln add "$solutionDir/$testsDir/$testProjectName/$testProjectName.csproj"
}
