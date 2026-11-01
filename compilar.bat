@echo off
chcp 65001 >nul
echo Compilando CustomISLC.exe (64-bit)...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" ^
  /target:winexe ^
  /platform:x64 ^
  /optimize+ ^
  /utf8output ^
  /win32manifest:app.manifest ^
  /r:System.dll ^
  /r:System.Core.dll ^
  /r:System.Drawing.dll ^
  /r:System.Windows.Forms.dll ^
  /r:System.Web.Extensions.dll ^
  /out:CustomISLC.exe ^
  Program.cs NativeMethods.cs Config.cs MainForm.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCESSO] CustomISLC.exe compilado com sucesso!
) else (
    echo.
    echo [ERRO] Falha na compilacao.
)
pause
