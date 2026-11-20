@echo off
setlocal
chcp 65001 >nul

echo ========================================
echo   Desinstalador de StickyBar
echo ========================================
echo.
echo Desinstalando StickyBar...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0StickyBarInstaller.ps1" -Action Uninstall

if errorlevel 1 (
    echo.
    echo [ERROR] No se pudo completar la desinstalacion.
    pause
    exit /b 1
)

echo.
echo Desinstalacion completada correctamente.
pause
