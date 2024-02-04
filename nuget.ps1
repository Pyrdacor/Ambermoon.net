$ErrorActionPreference = 'Stop';

if ($isWindows) {
  Write-Host Copy nuget packages
  mkdir "nuget"
  xcopy /Y /I "Ambermoon.Common\bin\Release\*.nupkg" "nuget\*"
  xcopy /Y /I "Ambermoon.Data.Legacy\bin\Release\*.nupkg" "nuget\*"
  xcopy /Y /I "Ambermoon.Data.Common\bin\Release\*.nupkg" "nuget\*"
  xcopy /Y /I "Ambermoon.Data.FileSystems\bin\Release\*.nupkg" "nuget\*"
  xcopy /Y /I "Ambermoon.Data.GameDataRepository\bin\Release\*.nupkg" "nuget\*"
}