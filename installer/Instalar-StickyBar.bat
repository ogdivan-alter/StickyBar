@echo off
setlocal
chcp 65001 >nul

echo ========================================
echo   Instalador de StickyBar
echo ========================================
echo.

choice /C SN /M "¿Desea iniciar StickyBar automaticamente con Windows?"
if errorlevel 2 (
    set "AUTOSTART_ARG="
) else (
    set "AUTOSTART_ARG=-AutoStart"
)

echo.
echo Instalando StickyBar...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0StickyBarInstaller.ps1" -Action Install %AUTOSTART_ARG%

if errorlevel 1 (
    echo.
    echo [ERROR] No se pudo completar la instalacion.
    pause
    exit /b 1
)

echo.
echo Instalacion completada correctamente.
pause
