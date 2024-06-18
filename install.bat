@echo off
setlocal

echo INFO: installing windows

set CPP_RELEASE_VER=v1.3.0
set CURR_DIR=%cd%
set PARENT_DIR=plugin-srcs\Assets\Plugin

set LIB=shakoc.dll
if not exist %PARENT_DIR%\Libs (
    echo Creating directory %PARENT_DIR%\Libs
    mkdir %PARENT_DIR%\Libs
    echo Downloading %LIB%
    powershell -Command "Invoke-WebRequest -Uri https://github.com/toppers/hakoniwa-core-cpp-client/releases/download/%CPP_RELEASE_VER%/%LIB% -OutFile %LIB%" || (
        echo ERROR: failed to download
        if "%CALLER%"=="" (
            echo Press any key to exit...
            pause >nul
        )
        exit /b 1
    )
    echo Moving %LIB% to %PARENT_DIR%\Libs\
    move %LIB% %PARENT_DIR%\Libs\
    rem REMOVE gRPC codes
    echo Removing gRPC codes
    rmdir /s /q plugin-srcs\Assets\Plugin\src\PureCsharp\Gen*
    echo Installation completed successfully.
) else (
    echo %PARENT_DIR%\Libs already exists. Skipping download and setup.
)

if "%CALLER%"=="" (
    echo Press any key to exit...
    pause >nul
)

endlocal
exit /b 0
