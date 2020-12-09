$ErrorActionPreference = 'Stop';

if ($isWindows) {
  Write-Host Pack zip for Windows
  7z a Ambermoon.net-Windows.zip "Ambermoon.net\bin\Any CPU\Release\netcoreapp3.1\win-x64\publish\Ambermoon.net.exe"
} else {
  Write-Host Pack tar.gz for Linux
  7z a Ambermoon.net-Linux.tar "./Ambermoon.net/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.net.exe"
  7z a Ambermoon.net-Linux.tar.gz Ambermoon.net-Linux.tar
  rm Ambermoon.net-Linux.tar
}