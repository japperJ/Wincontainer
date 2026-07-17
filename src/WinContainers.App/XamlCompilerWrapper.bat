@echo off
setlocal
set TOOLDIR=C:\Users\jptrs\.nuget\packages\microsoft.windowsappsdk.winui\2.1.0\tools\net472

:: Ensure en-DK satellite assembly exists (copy from en)
if not exist "%TOOLDIR%\en-DK\Microsoft.UI.Xaml.Markup.Compiler.resources.dll" (
    mkdir "%TOOLDIR%\en-DK" 2>nul
    copy /y "%TOOLDIR%\en\Microsoft.UI.Xaml.Markup.Compiler.resources.dll" "%TOOLDIR%\en-DK\" >nul 2>&1
)

set EXEPATH=%~1
set INPUT=%~2
set OUTPUT=%~3
"%EXEPATH%" "%INPUT%" "%OUTPUT%"
set EXITCODE=%ERRORLEVEL%
if %EXITCODE% NEQ 0 (
    if exist "%OUTPUT%" del /f "%OUTPUT%"
)
exit /b %EXITCODE%