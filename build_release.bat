@echo off
rem Buduje wersje do publikacji: TerrariaArsenal_release.zip w tym folderze.
rem Roznice wobec build.bat: bez kodu testowego QUPA (RELEASE) i bez grafik/dzwiekow Terrarii w DLL
rem (mod czyta je z Terrarii zainstalowanej u gracza). W zipie: TerrariaArsenal.dll i README.md (bez folderu).
rem Obok powstaje TerrariaArsenal_release.sha256.txt - skrot zipa potrzebny do wpisu w ModLinks (Lumafly).
setlocal
cd /d "%~dp0"
set M=..\..
set STAGEROOT=%TEMP%\TerrariaArsenal_release
set STAGE=%STAGEROOT%\TerrariaArsenal
set OUT=%STAGE%\TerrariaArsenal.dll
set ZIP=TerrariaArsenal_release.zip
if exist "%STAGEROOT%" rmdir /s /q "%STAGEROOT%"
mkdir "%STAGE%"
if exist "%ZIP%" del "%ZIP%"
if exist build_release.log del build_release.log

> build_release.rsp echo /codepage:65001 /nologo /noconfig /nostdlib+ /target:library /optimize+ /define:RELEASE /out:"%OUT%"
for %%d in (mscorlib System System.Core netstandard Assembly-CSharp Assembly-CSharp-firstpass PlayMaker MMHOOK_Assembly-CSharp MMHOOK_PlayMaker UnityEngine UnityEngine.CoreModule UnityEngine.Physics2DModule UnityEngine.AudioModule UnityEngine.ImageConversionModule UnityEngine.TextRenderingModule UnityEngine.IMGUIModule UnityEngine.UIModule UnityEngine.InputLegacyModule UnityEngine.UI Newtonsoft.Json MonoMod.RuntimeDetour MonoMod.Utils) do >> build_release.rsp echo /r:"%M%\%%d.dll"
rem Tylko wlasne grafiki moda (nie z Terrarii).
for %%f in (Dust.png P_Spark.png) do >> build_release.rsp echo /resource:"res\%%f",TerrariaArsenal.%%f
>> build_release.rsp echo src\*.cs

set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Data
if exist "%UNITY%\NetCoreRuntime\dotnet.exe" if exist "%UNITY%\DotNetSdkRoslyn\csc.dll" (
    "%UNITY%\NetCoreRuntime\dotnet.exe" "%UNITY%\DotNetSdkRoslyn\csc.dll" @build_release.rsp > build_release.log 2>&1
    goto done
)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" @build_release.rsp > build_release.log 2>&1

:done
if exist build_release.log type build_release.log
if not exist "%OUT%" (
    echo.
    echo BLAD: kompilacja sie nie udala. Wyslij mi plik build_release.log z tego folderu.
    pause
    exit /b 1
)
copy /y README.md "%STAGE%\README.md" > nul
rem Lumafly/Scarab rozpakowuja zip do Mods\TerrariaArsenal, wiec DLL i README leza w zipie bez folderu.
powershell -NoProfile -Command "Compress-Archive -Force -Path '%STAGE%\*' -DestinationPath '%ZIP%'"
powershell -NoProfile -Command "(Get-FileHash -Algorithm SHA256 '%ZIP%').Hash" > TerrariaArsenal_release.sha256.txt
rmdir /s /q "%STAGEROOT%"
echo.
if exist "%ZIP%" (
    echo GOTOWE: %ZIP% - to wysylasz graczom / wrzucasz na strone.
) else (
    echo BLAD: nie udalo sie spakowac zipa.
)
pause
