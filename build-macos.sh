dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r osx-x64 --no-restore --self-contained
dotnet publish -c Release ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -r osx-arm64 --no-restore --self-contained
dotnet publish -c Release ./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj -r osx-x64 --no-restore
sudo xcode-select -p
sudo xcode-select -s /Applications/Xcode.app
mkdir -p ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/
cp -r ./Ambermoon.net/Mac/* ./bundle/Ambermoon.net/
cp "./Ambermoon.net/bin/Release/net8.0/osx-x64/publish/Ambermoon.net" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/Ambermoon.net
cp "./Ambermoon.net/versions.dat" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/Resources/
cp "./Ambermoon.net/diffs.dat" ./bundle/Ambermoon.net/Ambermoon.net.app/Contents/Resources/
cp -r ./Package/* ./bundle/Ambermoon.net/
codesign -s - --force --verbose --deep --no-strict ./bundle/Ambermoon.net/Ambermoon.net.app
7z a Ambermoon.net-Mac.zip ./bundle/Ambermoon.net/ -mx9
cp -r ./bundle ./bundle-arm
rm ./bundle-arm/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/Ambermoon.net
cp "./Ambermoon.net/bin/Release/net8.0/osx-arm64/publish/Ambermoon.net" ./bundle-arm/Ambermoon.net/Ambermoon.net.app/Contents/MacOS/Ambermoon.net
codesign -s - --force --verbose --deep --no-strict ./bundle-arm/Ambermoon.net/Ambermoon.net.app
7z a Ambermoon.net-Mac-ARM.zip ./bundle-arm/Ambermoon.net/ -mx9
