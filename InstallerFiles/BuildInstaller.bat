@echo off

REM Arguments
REM %1: version
REM %2: architecture

set z=Zsnd-UI-%1-%2

REM clean the publish folder
del %z%.7z %zf%.7z 2>nul

REM create installer
REM for /d %%a in (*-*) do if not "%%~a"=="en-us" rmdir /q /s %%a 2>nul
call :WriteCfg %1 >InstallerFiles\config.txt
InstallerFiles\7z.exe a -t7z %z%.7z ./ -xr!InstallerFiles -x!Zsnd-UI-*
REM -x!*.pdb -x!createdump.exe -x!RestartAgent.exe -x!*.winmd
call :BuildInstaller %z%

EXIT

:BuildInstaller
copy /b InstallerFiles\7zSD.sfx + InstallerFiles\config.txt + %1.7z %1.exe
EXIT /b

:WriteCfg
echo ;!@Install@!UTF-8!
echo Title="Zsnd-UI v%1"
echo ExtractPathText="Please select a folder to install Zsnd-UI to:"
echo ExtractPathTitle="Zsnd-UI v%1"
echo ExtractTitle="Extracting..."
echo GUIFlags="128"
echo InstallPath="%%UserProfile%%\Desktop\Zsnd-UI"
echo OverwriteMode="1"
echo ;!@InstallEnd@!
EXIT /b
