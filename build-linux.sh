mkdir publish-linux
cp ./Ambermoon.net/versions.dat publish-linux
cp ./Ambermoon.net/diffs.dat publish-linux
dotnet publish -c Debug ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-x64 --nologo --self-contained -o ./publish-linux
dotnet publish -c Debug ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-x64 --nologo --self-contained -o ./publish-linux
dotnet publish -c Debug ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-x64 --nologo --self-contained -o ./publish-linux
cd publish-v
Ambermoon.ConcatFiles versions versions.dat patcher AmbermoonPatcher Ambermoon.net
rm ./versions.dat
rm ./diffs.dat
rm ./Ambermoon.ConcatFiles