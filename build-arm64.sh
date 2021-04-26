mkdir publish-arm64
cp versions.dat publish-arm64
dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -r linux-arm64 --nologo --self-contained -o ./publish-arm64
dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -p:PublishSingleFile=true -r linux-arm64 -o ./publish-arm64
cd publish-arm64
./Ambermoon.ConcatFiles ./versions.dat ./Ambermoon.net
rm ./versions.dat
rm ./Ambermoon.ConcatFiles