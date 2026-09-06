@echo off
setlocal
cd /d "%~dp0"
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo Could not find the C# compiler that ships with Windows: %CSC%
    pause
    exit /b 1
)
if not exist bin mkdir bin
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 ^
    /win32icon:src\taskbarpick.ico /resource:src\taskbarpick.ico,taskbarpick.ico ^
    /out:bin\taskbarpick.exe ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll src\*.cs
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)
echo Built bin\taskbarpick.exe
