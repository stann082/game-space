# Game Space

A console app to get the overall size of all games installed on the system

## Installation

Execute the build command

```powershell
nant restore clean build deploy
```

## Usage

By default the app is deployed to %APPDATA%\GameSpace. I suggest adding that folder to environment Path. On deployment a powershell script **game-space.ps1** is generated and placed at the root of that directory, it will run the executable inside %APPDATA%\GameSpace\bin. Once the root dir is in the path, you should be able to just call the script from anywhere in powershell
```powershell
game-space.ps1
```

