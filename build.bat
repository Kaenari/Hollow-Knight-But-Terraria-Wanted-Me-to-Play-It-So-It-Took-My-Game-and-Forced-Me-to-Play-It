@echo off
rem Kompiluje moda Terraria Arsenal do TerrariaArsenal.dll w tym folderze.
rem Uruchom dwuklikiem. Gra musi byc zamknieta (inaczej DLL jest zablokowany).
rem Opcje kompilatora ida przez plik build.rsp, bo linia polecen Windows ma limit 8191 znakow.
setlocal
cd /d "%~dp0"
set M=..\..
set OUT=TerrariaArsenal.dll
if exist "%OUT%" del "%OUT%"
if exist build.log del build.log

> build.rsp echo /codepage:65001 /nologo /noconfig /nostdlib+ /target:library /optimize+ /out:"%OUT%"
for %%d in (mscorlib System System.Core netstandard Assembly-CSharp Assembly-CSharp-firstpass PlayMaker MMHOOK_Assembly-CSharp MMHOOK_PlayMaker UnityEngine UnityEngine.CoreModule UnityEngine.Physics2DModule UnityEngine.AudioModule UnityEngine.ImageConversionModule UnityEngine.TextRenderingModule UnityEngine.IMGUIModule UnityEngine.UIModule UnityEngine.InputLegacyModule UnityEngine.UI Newtonsoft.Json MonoMod.RuntimeDetour MonoMod.Utils) do >> build.rsp echo /r:"%M%\%%d.dll"
for %%f in (res\*) do >> build.rsp echo /resource:"res\%%~nxf",TerrariaArsenal.%%~nxf
>> build.rsp echo src\*.cs

set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Data
if exist "%UNITY%\NetCoreRuntime\dotnet.exe" if exist "%UNITY%\DotNetSdkRoslyn\csc.dll" (
    echo Kompilator: Roslyn z Unity 2022.3.22f1
    "%UNITY%\NetCoreRuntime\dotnet.exe" "%UNITY%\DotNetSdkRoslyn\csc.dll" @build.rsp > build.log 2>&1
    goto done
)

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
echo Kompilator: wbudowany w Windows (%CSC%)
"%CSC%" @build.rsp > build.log 2>&1

:done
if exist build.log type build.log
if exist "%OUT%" (
    echo.
    echo GOTOWE: %OUT% zbudowany. Mozesz uruchomic gre z modami.
) else (
    echo.
    echo BLAD: kompilacja sie nie udala. Wyslij mi plik build.log z tego folderu.
)
pause
