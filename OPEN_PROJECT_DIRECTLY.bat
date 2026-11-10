@echo off
setlocal
set "PROJECT_DIR=%~dp0"
set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"
if not exist "%PROJECT_DIR%Assets\Project_\Scenes\MainMenu.unity" (
  echo ERROR: This folder is not the Unity project root.
  echo It must contain Assets, Packages, and ProjectSettings.
  pause
  exit /b 1
)
if not exist "%PROJECT_DIR%ProjectSettings\ProjectVersion.txt" (
  echo ERROR: ProjectSettings\ProjectVersion.txt was not found.
  pause
  exit /b 1
)
if not exist "%UNITY_EXE%" (
  echo Unity 6000.6.8f1 was not found in the default Unity Hub location.
  echo Please open Unity Hub and use Add ^> Add project from disk.
  echo Select this folder: %PROJECT_DIR%
  pause
  exit /b 2
)
start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%"
endlocal
