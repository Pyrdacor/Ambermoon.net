# Compile and run Ambermoon.net on Windows with VS

- Clone the git repository `git clone https://github.com/Pyrdacor/Ambermoon.net`
- Download the game data from https://github.com/Pyrdacor/Ambermoon
  - You can choose any language and either the extracted or ADF download (ADF might not polute your directories as much)
  - Extract the files to the sub-project folder `./Ambermoon.net/Ambermoon.net`
    - If you choose ADF, extract the ADF files to that folder
    - If you choose extracted, extract the contents of the Amberfiles folder to that folder
- Open the Ambermoon.net solution in Visual Studio
- Select the project "Ambermoon.net" as the main project
- Build and run


## Other dependencies

- Make sure you have the latest .NET 6 SDK installed
- If you want to build and test the Android project make sure to install the android workload (`dotnet workload install android`) and enable the projects. They are WIP so they might not work as expected yet!