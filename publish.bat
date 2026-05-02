@echo off
setlocal enabledelayedexpansion
title CassieWordCheck Publish

set "PROJ_DIR=%~dp0"
set "PUBLISH_DIR=%PROJ_DIR%dist"
set "APP_NAME=CassieWordCheck"
set "RID=win-x64"
set "OUTPUT_NAME=CASSIE CWC Tool（CASSIE Word Check）"
set "OUTPUT_DIR=%PROJ_DIR%%OUTPUT_NAME%"
set "ZIP_FILE=%PROJ_DIR%%OUTPUT_NAME%.zip"

echo ========================================
echo   CassieWordCheck - Publish Package
echo ========================================
echo   [1] Build + Create folder
echo   [2] Zip only (keep folder as-is)
echo   [3] Full rebuild + zip
echo ========================================
echo.
choice /c 123 /n /m "Choose (1/2/3): "

if errorlevel 3 goto :full
if errorlevel 2 goto :ziponly
if errorlevel 1 goto :build

:build
REM Clean dist but keep output folder
if exist "%PUBLISH_DIR%" (
    echo [Clean] Removing old dist...
    rmdir /s /q "%PUBLISH_DIR%" || goto :err
)

echo [1/3] Restoring packages...
call dotnet restore "%PROJ_DIR%%APP_NAME%.csproj" --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [2/3] Publishing single-file...
call dotnet publish "%PROJ_DIR%%APP_NAME%.csproj" -c Release -r %RID% -o "%PUBLISH_DIR%" --verbosity quiet
if %errorlevel% neq 0 goto :err

REM Create output folder (don't delete if exists, so your edits survive)
if not exist "%OUTPUT_DIR%" (
    echo [3/3] Creating output folder...
    xcopy /E /I /Q "%PUBLISH_DIR%" "%OUTPUT_DIR%" >nul
    if exist "%OUTPUT_DIR%\*.pdb" del /q "%OUTPUT_DIR%\*.pdb" 2>nul
    echo.
    echo Folder created: %OUTPUT_NAME%
    echo You can now edit files inside it, then run option [2] to re-zip.
) else (
    echo [Skip] Output folder already exists, keeping your edits.
    echo To rebuild from scratch, run option [3].
)
goto :end

:ziponly
echo [Zipping] Packing %OUTPUT_NAME% ...
if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" 2>nul
if not exist "%OUTPUT_DIR%" (
    echo [Error] Output folder not found. Run option [1] first.
    pause
    exit /b 1
)
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"
echo.
echo Done: %ZIP_FILE%
goto :end

:full
echo [Full rebuild] ...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%" 2>nul
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%" 2>nul
if exist "%ZIP_FILE%" del /q "%ZIP_FILE%" 2>nul

call dotnet restore "%PROJ_DIR%%APP_NAME%.csproj" --verbosity quiet
call dotnet publish "%PROJ_DIR%%APP_NAME%.csproj" -c Release -r %RID% -o "%PUBLISH_DIR%" --verbosity quiet
if %errorlevel% neq 0 goto :err

xcopy /E /I /Q "%PUBLISH_DIR%" "%OUTPUT_DIR%" >nul
if exist "%OUTPUT_DIR%\*.pdb" del /q "%OUTPUT_DIR%\*.pdb" 2>nul

powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"
echo.
echo Full rebuild done!
echo   Folder: %OUTPUT_DIR%
echo   Zip:    %ZIP_FILE%
goto :end

:err
echo.
echo [FAILED] Error during packaging.
pause
exit /b 1

:end
echo.
echo Press any key to exit...
pause >nul
endlocal
