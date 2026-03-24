New-Item -ItemType Directory -Force -Path "Models/Entities/HealthRecords"
New-Item -ItemType Directory -Force -Path "DTOs/HealthRecords"
New-Item -ItemType Directory -Force -Path "Validations/HealthRecords"
New-Item -ItemType Directory -Force -Path "Repositories/HealthRecords"
New-Item -ItemType Directory -Force -Path "Services/HealthRecords"
New-Item -ItemType Directory -Force -Path "Controllers/HealthRecords"

Move-Item -Path "Models/Entities/BloodPressureRecord.cs" -Destination "Models/Entities/HealthRecords"
Move-Item -Path "DTOs/BloodPressures" -Destination "DTOs/HealthRecords"
Move-Item -Path "Validations/BloodPressures" -Destination "Validations/HealthRecords"
Move-Item -Path "Repositories/*BloodPressureRepository.cs" -Destination "Repositories/HealthRecords"
Move-Item -Path "Services/*BloodPressureService.cs" -Destination "Services/HealthRecords"
Move-Item -Path "Controllers/BloodPressuresController.cs" -Destination "Controllers/HealthRecords"

# Update shifted files
Get-ChildItem -Path "Models/Entities/HealthRecords", "DTOs/HealthRecords", "Validations/HealthRecords", "Repositories/HealthRecords", "Services/HealthRecords", "Controllers/HealthRecords" -Recurse -Filter *.cs | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # 1. Update Namespaces
    $content = $content -replace 'namespace SeasonsCare.Api.Models.Entities(\r?\n)', "namespace SeasonsCare.Api.Models.Entities.HealthRecords`$1"
    $content = $content -replace 'namespace SeasonsCare.Api.DTOs.BloodPressures(\r?\n)', "namespace SeasonsCare.Api.DTOs.HealthRecords.BloodPressures`$1"
    $content = $content -replace 'namespace SeasonsCare.Api.Validations.BloodPressures(\r?\n)', "namespace SeasonsCare.Api.Validations.HealthRecords.BloodPressures`$1"
    $content = $content -replace 'namespace SeasonsCare.Api.Repositories(\r?\n)', "namespace SeasonsCare.Api.Repositories.HealthRecords`$1"
    $content = $content -replace 'namespace SeasonsCare.Api.Services(\r?\n)', "namespace SeasonsCare.Api.Services.HealthRecords`$1"
    $content = $content -replace 'namespace SeasonsCare.Api.Controllers(\r?\n)', "namespace SeasonsCare.Api.Controllers.HealthRecords`$1"
    
    # 2. Update Usings
    $content = $content -replace 'using SeasonsCare.Api.Models.Entities;', "using SeasonsCare.Api.Models.Entities;`r`nusing SeasonsCare.Api.Models.Entities.HealthRecords;"
    $content = $content -replace 'using SeasonsCare.Api.DTOs.BloodPressures;', "using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;"
    $content = $content -replace 'using SeasonsCare.Api.Repositories;', "using SeasonsCare.Api.Repositories;`r`nusing SeasonsCare.Api.Repositories.HealthRecords;"
    $content = $content -replace 'using SeasonsCare.Api.Services;', "using SeasonsCare.Api.Services;`r`nusing SeasonsCare.Api.Services.HealthRecords;"
    
    Set-Content -Path $_.FullName -Value $content -NoNewline
}

# Update Data/ApplicationDbContext.cs
$dbContext = Get-Content Data/ApplicationDbContext.cs -Raw
$dbContext = $dbContext -replace 'using SeasonsCare.Api.Models.Entities;', "using SeasonsCare.Api.Models.Entities;`r`nusing SeasonsCare.Api.Models.Entities.HealthRecords;"
Set-Content -Path "Data/ApplicationDbContext.cs" -Value $dbContext -NoNewline

# Update Program.cs
$program = Get-Content Program.cs -Raw
$program = $program -replace 'using SeasonsCare.Api.Repositories;', "using SeasonsCare.Api.Repositories;`r`nusing SeasonsCare.Api.Repositories.HealthRecords;"
$program = $program -replace 'using SeasonsCare.Api.Services;', "using SeasonsCare.Api.Services;`r`nusing SeasonsCare.Api.Services.HealthRecords;"
Set-Content -Path "Program.cs" -Value $program -NoNewline
