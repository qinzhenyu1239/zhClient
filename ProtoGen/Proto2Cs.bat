@echo off
set SOURCE_PATH=.\Proto
set PROTOGEN_PATH=..\ProtoGen\protogen.exe
set TARGET_PATH=.\Cs
del %TARGET_PATH%\*.* /f /s /q
echo -------------------------------------------------------------
for /f "delims=" %%i in ('dir /b "%SOURCE_PATH%\*.proto"') do (
    
    echo conv：%%i to %%~ni.cs
    %PROTOGEN_PATH% -i:%SOURCE_PATH%\%%i -o:%TARGET_PATH%\%%~ni.cs -ns:ProtoData
    
)

echo conv end

pause