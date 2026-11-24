@echo off
setlocal
echo ============================================================
echo   Building GeoMine Explorer v1.0
echo ============================================================

set CSC_PATH=
where csc.exe >nul 2>nul
if %ERRORLEVEL% equ 0 (
    set CSC_PATH=csc.exe
) else (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
        set CSC_PATH="%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    ) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
        set CSC_PATH="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )
)

if "%CSC_PATH%"=="" (
    echo [ERROR] C# compiler csc.exe was not found on your system.
    pause
    exit /b 1
)

echo Using compiler: %CSC_PATH%
%CSC_PATH% /target:winexe /optimize+ /platform:anycpu /out:GeoMineExplorer.exe /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll src\Models.cs src\GeoAlgorithms.cs src\CanvasViews.cs src\MainForm.cs src\Program.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] GeoMineExplorer.exe compiled successfully!
) else (
    echo [ERROR] Compilation failed.
)

endlocal
