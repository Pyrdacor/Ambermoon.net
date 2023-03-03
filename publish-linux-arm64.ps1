Write-Host Publish Linux-arm64 executable
dotnet clean "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet build -c ReleaseES "./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj" -r win-x64
dotnet build -c ReleaseES "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet publish -c ReleaseES ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --no-restore --self-contained
dotnet publish -c ReleaseES ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --nologo --self-contained
Write-Host Pack tar for Linux
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/ReleaseES/net7.0/win-x64/Ambermoon.ConcatFiles.exe" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/ReleaseES/net7.0/linux-arm64/publish/Ambermoon.net"'

7z a Ambermoon.net-Linux-arm64.tar "./Ambermoon.net/bin/ReleaseES/net7.0/linux-arm64/publish/Ambermoon.net" "./AmbermoonPatcher/bin/ReleaseES/net7.0/linux-arm64/publish/AmbermoonPatcher" "./Package/*"
7z a Ambermoon.net-Linux-arm64.tar.gz Ambermoon.net-Linux-arm64.tar -mx9
rm Ambermoon.net-Linux-arm64.tar