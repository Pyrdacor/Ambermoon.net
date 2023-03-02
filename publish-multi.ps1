Write-Host Publish Windows executables
dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -r win-x64 --nologo --self-contained
dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x86 --nologo --self-contained
dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x64 --nologo --self-contained
dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x86 --no-restore --nologo --self-contained
dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r win-x64 --no-restore --nologo --self-contained
Write-Host Pack zips for Windows
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/Release/net7.0/win-x64/publish/Ambermoon.net.exe"'
cmd /c copy "Ambermoon.net\bin\Release\net7.0\win-x64\publish\Ambermoon.net.exe" "Ambermoon.net\Ambermoon.net.exe"
7z a Ambermoon.net-Windows-x64.zip "./Ambermoon.net/Ambermoon.net.exe" "./AmbermoonPatcher/bin/Release/net7.0/win-x64/publish/AmbermoonPatcher.exe" "./Ambermoon.net/x64/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/Release/net7.0/win-x86/publish/Ambermoon.net.exe"'
cmd /c copy "Ambermoon.net\bin\Release\net7.0\win-x86\publish\Ambermoon.net.exe" "Ambermoon.net\Ambermoon.net.exe"
7z a Ambermoon.net-Windows-x86.zip "./Ambermoon.net/Ambermoon.net.exe" "./AmbermoonPatcher/bin/Release/net7.0/win-x86/publish/AmbermoonPatcher.exe" "./Ambermoon.net/x86/api-ms-win-core-winrt-l1-1-0.dll" "./Package/*" -mx9

Write-Host Publish Linux-x64 executable
dotnet build -c Release "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-x64 --no-restore --self-contained
dotnet publish -c Release ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-x64 --nologo --self-contained
Write-Host Pack tar for Linux
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/Release/net7.0/linux-x64/publish/Ambermoon.net"'
7z a Ambermoon.net-Linux-x64.tar "./Ambermoon.net/bin/Release/net7.0/linux-x64/publish/Ambermoon.net" "./AmbermoonPatcher/bin/Release/net7.0/linux-x64/publish/AmbermoonPatcher" "./Package/*"
7z a Ambermoon.net-Linux-x64.tar.gz Ambermoon.net-Linux-x64.tar -mx9
rm Ambermoon.net-Linux-x64.tar

Write-Host Publish Linux-arm64 executable
dotnet clean "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet build -c ReleaseES "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet publish -c ReleaseES ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --no-restore --self-contained
dotnet publish -c ReleaseES ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --nologo --self-contained
Write-Host Pack tar for Linux
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/ReleaseES/net7.0/linux-arm64/publish/Ambermoon.net"'
7z a Ambermoon.net-Linux-arm64.tar "./Ambermoon.net/bin/ReleaseES/net7.0/linux-arm64/publish/Ambermoon.net" "./AmbermoonPatcher/bin/ReleaseES/net7.0/linux-arm64/publish/AmbermoonPatcher" "./Package/*"
7z a Ambermoon.net-Linux-arm64.tar.gz Ambermoon.net-Linux-arm64.tar -mx9
rm Ambermoon.net-Linux-arm64.tar