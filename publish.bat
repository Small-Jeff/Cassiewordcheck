@echo off
setlocal enabledelayedexpansion
title CassieWordCheck Publish

set "PROJ_DIR=%~dp0"
set "PUBLISH_DIR=%PROJ_DIR%dist"
set "APP_NAME=CassieWordCheck"
set "RID=win-x64"

echo ========================================
echo   CassieWordCheck - Publish Package
echo   Output: %PUBLISH_DIR%
echo   Runtime: %RID% (Self-contained)
echo   Data folder: data\ (统一封装)
echo ========================================
echo.

REM Clean old dist
if exist "%PUBLISH_DIR%" (
    echo [Clean] Removing old output directory...
    rmdir /s /q "%PUBLISH_DIR%" || goto :err
)

echo [1/4] Restoring packages...
call dotnet restore "%PROJ_DIR%%APP_NAME%.csproj" --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [2/4] Building Release...
call dotnet build "%PROJ_DIR%%APP_NAME%.csproj" -c Release --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [3/4] Publishing single-file...
call dotnet publish "%PROJ_DIR%%APP_NAME%.csproj" -c Release -r %RID% -o "%PUBLISH_DIR%" --verbosity normal
if %errorlevel% neq 0 goto :err

echo [4/4] Verifying output structure...
if exist "%PUBLISH_DIR%*.pdb" del /q "%PUBLISH_DIR%*.pdb" 2>nul

REM Show final dist structure
echo.
echo ====== Output structure ======
if exist "%PUBLISH_DIR%CassieWordCheck.exe" (
    echo   [exe]  CassieWordCheck.exe
)
if exist "%PUBLISH_DIR%data\" (
    echo   [data] data\
    dir /b "%PUBLISH_DIR%data\" 2>nul
)
if exist "%PUBLISH_DIR%Resources\" (
    echo   [res]  Resources\Locales\
)
echo ===============================
echo.
echo ====== Done! Output in dist\ folder ======
echo.
start "" "%PUBLISH_DIR%"
goto :end

:err
echo.
echo [FAILED] Error during packaging. Check output above.
pause
exit /b 1

:end
echo Press any key to exit...
pause >nul
endlocal
