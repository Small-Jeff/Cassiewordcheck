@echo off
setlocal enabledelayedexpansion
title CassieWordCheck Publish

set PROJ_DIR=%~dp0
set PUBLISH_DIR=%PROJ_DIR%dist
set APP_NAME=CassieWordCheck
set RID=win-x64

echo ========================================
echo   CassieWordCheck - Publish Package
echo   Runtime: %RID% (Self-contained)
echo   Version: 2.3.0
echo ========================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] dotnet not found.
    pause
    exit /b 1
)

pushd "%PROJ_DIR%"

if exist "%PUBLISH_DIR%" (
    echo [1/4] Cleaning...
    rmdir /s /q "%PUBLISH_DIR%" 2>nul
)

echo [2/4] Restoring...
call dotnet restore "%APP_NAME%.csproj" --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [3/4] Building...
call dotnet build "%APP_NAME%.csproj" -c Release --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [4/4] Publishing...
call dotnet publish "%APP_NAME%.csproj" -c Release -r %RID% -o "%PUBLISH_DIR%" --verbosity normal
if %errorlevel% neq 0 goto :err

if exist "%PUBLISH_DIR%\*.pdb" del /q "%PUBLISH_DIR%\*.pdb" 2>nul

echo.
echo ====== Output ======
dir /b "%PUBLISH_DIR%data\" 2>nul
if exist "%PUBLISH_DIR%Resources\" (
    echo   [res] Resources\Locales\
)
echo ====================
echo.
echo Done!
echo.
start "" "%PUBLISH_DIR%"
goto :end

:err
echo.
echo [FAILED]
pause
exit /b 1

:end
endlocal