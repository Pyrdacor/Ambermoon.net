$ErrorActionPreference = 'Stop';

if ($isWindows) {
  Write-Host Publish Windows executables
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x86 --no-restore --nologo --self-contained
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x64 --no-restore --nologo --self-contained
  Write-Host Pack standalone zips for Windows
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\net6.0\win-x64\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows.zip "./Ambermoon.net/Ambermoon.net.exe" "./Package/*" -mx9
  7z a Ambermoon.net-Windows7.zip "./Ambermoon.net/Ambermoon.net.exe" "./Ambermoon.net/x64/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\net6.0\win-x86\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows32Bit.zip "./Ambermoon.net/Ambermoon.net.exe" "./Package/*" -mx9
  7z a Ambermoon.net-Windows7-32Bit.zip "./Ambermoon.net/Ambermoon.net.exe" "./Ambermoon.net/x86/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9
} elseif ($isLinux) {
  Write-Host Publish Linux executable
  Set-Variable -Name UseGLES -Value false
  dotnet build -c Release "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-x64 --no-restore --self-contained
  dotnet publish -c Release "./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj" -r linux-x64 --no-restore
  Write-Host Pack tar for Linux
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList '"./versions.dat"','"./Ambermoon.net/bin/Any CPU/Release/net6.0/linux-x64/publish/Ambermoon.net"'
  7z a Ambermoon.net-Linux.tar "./Ambermoon.net/bin/Any CPU/Release/net6.0/linux-x64/publish/Ambermoon.net" "./Package/*"
  7z a Ambermoon.net-Linux.tar.gz Ambermoon.net-Linux.tar -mx9
  rm Ambermoon.net-Linux.tar
} else {
  Write-Host Publish Mac executable
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r osx-x64 --no-restore --self-contained
  dotnet publish -c Release "./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj" -r osx-x64 --no-restore
  Write-Host Pack zips for Mac
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/netcoreapp3.1/osx-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList '"./versions.dat"','"./Ambermoon.net/bin/Any CPU/Release/net6.0/osx-x64/publish/Ambermoon.net"'
  7z a Ambermoon.net-Mac.zip "./Ambermoon.net/bin/Any CPU/Release/net6.0/osx-x64/publish/Ambermoon.net" "./Package/*" -mx9
  mkdir -p ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  cp -r ./Ambermoon.net/Mac/* ./bundle/Ambermoon.net/
  cp "./Ambermoon.net/bin/Any CPU/Release/net6.0/osx-x64/publish/Ambermoon.net" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  cp -r ./Package/* ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  7z a Ambermoon.net-Mac-Bundle.zip ./bundle/Ambermoon.net/ -mx9
}
