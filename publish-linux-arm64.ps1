Write-Host Publish Linux-arm64 executable
Set-Variable -Name UseGLES -Value true
dotnet clean "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet build -c ReleaseWithAndroid "./Ambermoon.Renderer.OpenGL/Ambermoon.Renderer.OpenGL.csproj"
dotnet publish -c ReleaseWithAndroid ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --no-restore --self-contained
dotnet publish -c ReleaseWithAndroid ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r linux-arm64 --nologo --self-contained
Write-Host Pack tar for Linux
Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Release/net7.0/win-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList 'versions','"./versions.dat"','diffs','diffs.dat','"./Ambermoon.net/bin/ReleaseWithAndroid/net7.0/linux-arm64/publish/Ambermoon.net"'

7z a Ambermoon.net-Linux-arm64.tar "./Ambermoon.net/bin/ReleaseWithAndroid/net7.0/linux-arm64/publish/Ambermoon.net" "./AmbermoonPatcher/bin/ReleaseWithAndroid/net7.0/linux-arm64/publish/AmbermoonPatcher" "./Package/*"
7z a Ambermoon.net-Linux-arm64.tar.gz Ambermoon.net-Linux-arm64.tar -mx9
rm Ambermoon.net-Linux-arm64.tar