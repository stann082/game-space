# Game Space

A console app to get the overall size of all games installed on the system

## Installation

Execute the build command (make sure you have nant directory in your path)

```powershell
nant restore clean build deploy
```

## Usage

By default the app is deployed to %APPDATA%\GameSpace. I suggest adding that folder to your path. On deployment a powershell script **game-space.ps1** is generated and placed at the root of that directory, it will run the executable inside %APPDATA%\GameSpace\bin. Once the root dir is in the path, you should be able to just call the script from anywhere in powershell
```powershell
game-space.ps1
```
To remove empty or mostly empty directories that were left behind after the installation, run the same script with --cleanup arg and a folder name (you can enclose folder name in quotes, but it's optional)
```powershell
game-space.ps1 --cleanup Max Payne 3
```
