mkdir publish-win64
copy versions.dat publish-win64
dotnet publish -c Debug ./Ambermoon.net/Ambermoon.net.csproj -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true  -r win-x64 --nologo --self-contained -o ./publish-win64
cd publish-win64
copy /b Ambermoon.net.exe+versions.dat Ambermoon.net.exe
rm ./versions.dat
