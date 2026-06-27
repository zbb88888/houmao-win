@echo off
REM Houmao Windows Build Script
REM Usage: make.bat <command>

if "%1"=="" goto help
if "%1"=="help" goto help
if "%1"=="build" goto build
if "%1"=="release" goto release
if "%1"=="test" goto test
if "%1"=="publish" goto publish
if "%1"=="clean" goto clean
if "%1"=="check" goto check
if "%1"=="install" goto install
if "%1"=="run" goto run
if "%1"=="format" goto format
if "%1"=="restore" goto restore

echo Unknown command: %1
goto help

:help
echo.
echo Houmao Windows Build System
echo ==========================
echo.
echo Usage: make.bat ^<command^>
echo.
echo Commands:
echo   build       Build Debug configuration
echo   release     Build Release configuration
echo   test        Run unit tests
echo   publish     Publish single-file executable
echo   clean       Clean build artifacts
echo   check       Check project structure
echo   install     Check and build (Debug)
echo   run         Build and run the application
echo   format      Format code
echo   restore     Restore NuGet packages
echo   help        Show this help message
echo.
echo Examples:
echo   make.bat build
echo   make.bat release
echo   make.bat test
echo   make.bat publish
echo.
goto end

:build
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 build
goto end

:release
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 release
goto end

:test
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 test
goto end

:publish
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 publish
goto end

:clean
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 clean
goto end

:check
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 check
goto end

:install
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 install
goto end

:run
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 run
goto end

:format
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 format
goto end

:restore
powershell -ExecutionPolicy Bypass -File scripts\make.ps1 restore
goto end

:end
