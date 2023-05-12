$ErrorActionPreference = 'Stop';

if ($isWindows) {
  Write-Host Publish Windows executables
  dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -r win-x64 --nologo --self-contained
  dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x86 --nologo --self-contained
  dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x64 --no-restore --nologo --self-contained
  dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x86 --no-restore --nologo --self-contained
  dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x64 --no-restore --nologo --self-contained
  Write-Host Pack zips for Windows
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./Ambermoon.net/versions.dat"','diffs','./Ambermoon.net/diffs.dat','"./Ambermoon.net/bin/Any CPU/Release/net7.0/win-x64/publish/Ambermoon.net.exe"'
  cmd /c copy "Ambermoon.net\bin\Any CPU\Release\net7.0\win-x64\publish\Ambermoon.net.exe" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows.zip "./Ambermoon.net/Ambermoon.net.exe" "./AmbermoonPatcher/bin/Any CPU/Release/net7.0/win-x64/publish/AmbermoonPatcher.exe" "./Ambermoon.net/x64/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./Ambermoon.net/versions.dat"','diffs','./Ambermoon.net/diffs.dat','"./Ambermoon.net/bin/Any CPU/Release/net7.0/win-x86/publish/Ambermoon.net.exe"'
  cmd /c copy "Ambermoon.net\bin\Any CPU\Release\net7.0\win-x86\publish\Ambermoon.net.exe" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows32Bit.zip "./Ambermoon.net/Ambermoon.net.exe" "./AmbermoonPatcher/bin/Any CPU/Release/net7.0/win-x86/publish/AmbermoonPatcher.exe" "./Ambermoon.net/x86/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9
} elseif ($isLinux) {
  Write-Host Publish Linux executable
  Set-Variable -Name UseGLES -Value false
  dotnet build -c Release "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj" --no-restore
  dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-x64 --no-restore --self-contained
  dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -r linux-x64
  dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-x64 --nologo --self-contained
  Write-Host Pack tar for Linux
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/net7.0/linux-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./Ambermoon.net/versions.dat"','diffs','./Ambermoon.net/diffs.dat','"./Ambermoon.net/bin/Any CPU/Release/net7.0/linux-x64/publish/Ambermoon.net"'
  7z a Ambermoon.net-Linux.tar "./Ambermoon.net/bin/Any CPU/Release/net7.0/linux-x64/publish/Ambermoon.net" "./AmbermoonPatcher/bin/Any CPU/Release/net7.0/linux-x64/publish/AmbermoonPatcher" "./Package/*"
  7z a Ambermoon.net-Linux.tar.gz Ambermoon.net-Linux.tar -mx9
  rm Ambermoon.net-Linux.tar
} else {
  Write-Host Publish Mac executables
  dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r osx-x64 --no-restore --self-contained
  dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r osx-arm64 --no-restore --self-contained
  dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -r osx-x64 --no-restore
  Write-Host Pack zips for Mac
  sudo xcode-select -p
  sudo xcode-select -s /Applications/Xcode-13.2.1.app
  mkdir -p ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  cp -r ./Ambermoon.net/Mac/* ./bundle/Ambermoon.net/
  cp "./Ambermoon.net/bin/Any CPU/Release/net7.0/osx-x64/publish/Ambermoon.net" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  cp "./Ambermoon.net/versions.dat" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/Resources/
  cp "./Ambermoon.net/diffs.dat" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/Resources/
  cp -r ./Package/* ./bundle/Ambermoon.net/
  Start-Process -FilePath codesign -Wait -WorkingDirectory . -ArgumentList '-s','-','--force','--verbose','--deep','--no-strict','"./bundle/Ambermoon.net/Ambermoon.net.app"'
  7z a Ambermoon.net-Mac.zip ./bundle/Ambermoon.net/ -mx9
  cp -r ./bundle ./bundle-arm
  rm ./bundle-arm/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/Ambermoon.net
  cp "./Ambermoon.net/bin/Any CPU/Release/net7.0/osx-arm64/publish/Ambermoon.net" ./bundle-arm/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
  Start-Process -FilePath codesign -Wait -WorkingDirectory . -ArgumentList '-s','-','--force','--verbose','--deep','--no-strict','"./bundle-arm/Ambermoon.net/Ambermoon.net.app"'
  7z a Ambermoon.net-Mac-ARM.zip ./bundle-arm/Ambermoon.net/ -mx9
}
