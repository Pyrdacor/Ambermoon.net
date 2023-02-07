mkdir publish-arm64
copy versions.dat publish-arm64
dotnet publish -c Debug ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-arm64 --nologo --self-contained -o ./publish-arm64
dotnet publish -c Debug ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-arm64 --nologo --self-contained -o ./publish-arm64
dotnet publish -c Debug ./AmbermoonPatcher/AmbermoonPatcher.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r linux-arm64 --nologo --self-contained -o ./publish-arm64
cd publish-arm64
./Ambermoon.ConcatFiles versions versions.dat patcher AmbermoonPatcher Ambermoon.net
rm ./versions.dat
rm ./Ambermoon.ConcatFiles