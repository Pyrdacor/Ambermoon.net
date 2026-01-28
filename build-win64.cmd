mkdir publish-win64 2>nul
copy Ambermoon.net\versions.dat publish-win64\versions.dat
copy Ambermoon.net\diffs.dat publish-win64\diffs.dat
dotnet publish -c Debug ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x64 --nologo --self-contained -o ./publish-win64
dotnet publish -c Debug ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x64 --nologo --self-contained -o ./publish-win64
dotnet publish -c Debug ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x64 --nologo --self-contained -o ./publish-win64
cd publish-win64
Ambermoon.ConcatFiles.exe versions versions.dat diffs diffs.dat patcher AmbermoonPatcher.exe Ambermoon.net.exe
del versions.dat
del diffs.dat
del Ambermoon.ConcatFiles.exe
del AmbermoonPatcher.exe