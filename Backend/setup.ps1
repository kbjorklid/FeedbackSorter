# Set up the solution

$solutionName = "FeedbackSorter"
$solutionDir = "." # Current directory
$srcDir = "src"
$testsDir = "tests"

# Create the solution directory
Write-Host "Creating solution directory: $solutionName"
if (-not (Test-Path $solutionDir)) {
    New-Item -ItemType Directory -Path $solutionDir
}

# Create the solution file
Write-Host "Creating solution file: $solutionName.sln"
dotnet new sln -n $solutionName -o $solutionDir

# Create the source and tests directories
Write-Host "Creating source directory: $srcDir"
if (-not (Test-Path "$solutionDir/$srcDir")) {
    New-Item -ItemType Directory -Path "$solutionDir/$srcDir"
}

Write-Host "Creating tests directory: $testsDir"
if (-not (Test-Path "$solutionDir/$testsDir")) {
    New-Item -ItemType Directory -Path "$solutionDir/$testsDir"
}

# Create the C# projects
$projects = @(
    "FeedbackSorter.SharedKernel",
    "FeedbackSorter.Core",
    "FeedbackSorter.Application",
    "FeedbackSorter.Infrastructure",
    "FeedbackSorter.Presentation"
)

Write-Host "Creating C# projects:"
foreach ($projectName in $projects) {
    Write-Host "Creating project: $projectName"
    dotnet new classlib -n $projectName -o "$solutionDir/$srcDir/$projectName"
    dotnet sln $solutionDir/$solutionName.sln add "$solutionDir/$srcDir/$projectName/$projectName.csproj"
}

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
    dotnet new classlib -n $testProjectName -o "$solutionDir/$testsDir/$testProjectName"
    dotnet sln $solutionDir/$solutionName.sln add "$solutionDir/$testsDir/$testProjectName/$testProjectName.csproj"
}

# Add project references
Write-Host "Adding project references"

# Core references SharedKernel
Write-Host "Adding reference: FeedbackSorter.Core -> FeedbackSorter.SharedKernel"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Core/FeedbackSorter.Core.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.SharedKernel/FeedbackSorter.SharedKernel.csproj"

# Application references Core and SharedKernel
Write-Host "Adding reference: FeedbackSorter.Application -> FeedbackSorter.Core"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Application/FeedbackSorter.Application.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Core/FeedbackSorter.Core.csproj"
Write-Host "Adding reference: FeedbackSorter.Application -> FeedbackSorter.SharedKernel"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Application/FeedbackSorter.Application.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.SharedKernel/FeedbackSorter.SharedKernel.csproj"

# Infrastructure references Application, Core, and SharedKernel
Write-Host "Adding reference: FeedbackSorter.Infrastructure -> FeedbackSorter.Application"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Infrastructure/FeedbackSorter.Infrastructure.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Application/FeedbackSorter.Application.csproj"
Write-Host "Adding reference: FeedbackSorter.Infrastructure -> FeedbackSorter.Core"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Infrastructure/FeedbackSorter.Infrastructure.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Core/FeedbackSorter.Core.csproj"
Write-Host "Adding reference: FeedbackSorter.Infrastructure -> FeedbackSorter.SharedKernel"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Infrastructure/FeedbackSorter.Infrastructure.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.SharedKernel/FeedbackSorter.SharedKernel.csproj"

# Presentation references Application and SharedKernel
Write-Host "Adding reference: FeedbackSorter.Presentation -> FeedbackSorter.Application"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Presentation/FeedbackSorter.Presentation.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Application/FeedbackSorter.Application.csproj"
Write-Host "Adding reference: FeedbackSorter.Presentation -> FeedbackSorter.SharedKernel"
dotnet add "$solutionDir/$srcDir/FeedbackSorter.Presentation/FeedbackSorter.Presentation.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.SharedKernel/FeedbackSorter.SharedKernel.csproj"

# Add test project references
Write-Host "Adding test project references"

# Core.UnitTests references Core
Write-Host "Adding reference: FeedbackSorter.Core.UnitTests -> FeedbackSorter.Core"
dotnet add "$solutionDir/$testsDir/FeedbackSorter.Core.UnitTests/FeedbackSorter.Core.UnitTests.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Core/FeedbackSorter.Core.csproj"

# Application.UnitTests references Application
Write-Host "Adding reference: FeedbackSorter.Application.UnitTests -> FeedbackSorter.Application"
dotnet add "$solutionDir/$testsDir/FeedbackSorter.Application.UnitTests/FeedbackSorter.Application.UnitTests.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Application/FeedbackSorter.Application.csproj"

# Infrastructure.UnitTests references Infrastructure
Write-Host "Adding reference: FeedbackSorter.Infrastructure.UnitTests -> FeedbackSorter.Infrastructure"
dotnet add "$solutionDir/$testsDir/FeedbackSorter.Infrastructure.UnitTests/FeedbackSorter.Infrastructure.UnitTests.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Infrastructure/FeedbackSorter.Infrastructure.csproj"

# Presentation.UnitTests references Presentation
Write-Host "Adding reference: FeedbackSorter.Presentation.UnitTests -> FeedbackSorter.Presentation"
dotnet add "$solutionDir/$testsDir/FeedbackSorter.Presentation.UnitTests/FeedbackSorter.Presentation.UnitTests.csproj" reference "$solutionDir/$srcDir/FeedbackSorter.Presentation/FeedbackSorter.Presentation.csproj"

# Add xunit and NSubstitute package references to test projects
Write-Host "Adding xunit and NSubstitute package references to test projects"
$testProjects = @(
    "FeedbackSorter.Core.UnitTests",
    "FeedbackSorter.Application.UnitTests",
    "FeedbackSorter.Infrastructure.UnitTests",
    "FeedbackSorter.Presentation.UnitTests"
)

foreach ($testProjectName in $testProjects) {
    Write-Host "Adding package reference: xunit to $testProjectName"
    dotnet add "$solutionDir/$testsDir/$testProjectName/$testProjectName.csproj" package xunit

    Write-Host "Adding package reference: NSubstitute to $testProjectName"
    dotnet add "$solutionDir/$testsDir/$testProjectName/$testProjectName.csproj" package NSubstitute
}
