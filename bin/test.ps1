$ErrorActionPreference = "Stop"

$project = "Tests\SeasonsCare.Api.Tests.csproj"
$outDir = ".\bin\test-verify\"

dotnet test $project /p:UseAppHost=false /p:OutDir=$outDir
