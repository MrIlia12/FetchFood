@echo off
setlocal enabledelayedexpansion

rem === Параметры ===
set "SRC=%~1"
if not defined SRC set "SRC=."
for %%Z in ("%SRC%") do set "SRC=%%~fZ"
set "OUT=%~2"
if not defined OUT set "OUT=dump.txt"

rem === Белый список расширений ===
set "EXT_WHITELIST=.cs .csproj .sln .cshtml .razor .xaml .resx .config .props .targets .tt .json .editorconfig .ruleset .yml .yaml"

rem === Папки-исключения ===
set "SKIP_FOLDERS=\.git\ \.vs\ \bin\ \obj\ \node_modules\ \packages\"

echo ### DUMP START: %DATE% %TIME% ### > "%OUT%"

for /r "%SRC%" %%F in (*) do (
    set "FULL=%%~fF"
    set "SKIP=0"

    rem --- Проверка, не находится ли файл в исключённой папке ---
    for %%S in (%SKIP_FOLDERS%) do (
        echo !FULL! | findstr /i /c:"%%~S" >nul
        if !errorlevel! equ 0 set "SKIP=1"
    )

    rem --- Если нужно пропустить файл, идём дальше ---
    if "!SKIP!"=="1" (
        rem echo Пропуск служебного пути: !FULL!
    ) else (
        rem --- Проверяем расширение файла ---
        set "EXT=%%~xF"
        set "OK="
        for %%E in (%EXT_WHITELIST%) do (
            if /i "!EXT!"=="%%~E" set "OK=1"
        )

        if defined OK (
            set "REL=!FULL:%SRC%=!"
            if "!REL!"=="!FULL!" set "REL=%%~nxF"

            >>"%OUT%" echo -----BEGIN FILE-----
            >>"%OUT%" echo Path: !REL!
            >>"%OUT%" echo Base64:

            rem --- Кодирование файла ---
            certutil -f -encode "%%F" "%TEMP%\__enc.tmp" >nul
            for /f "usebackq delims=" %%A in ("%TEMP%\__enc.tmp") do (
                echo %%A | findstr /r /c:"^-*BEGIN" /c:"^-*END" >nul || >>"%OUT%" echo %%A
            )
            del /q "%TEMP%\__enc.tmp" >nul 2>&1

            >>"%OUT%" echo -----END FILE-----
            >>"%OUT%" echo.
        )
    )
)

>> "%OUT%" echo ### DUMP END ###
echo ✅ Готово: "%OUT%"
endlocal
