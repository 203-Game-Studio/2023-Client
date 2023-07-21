@echo off

del .\out_cs\*.* /f /s /q

cd proto

for /f "delims=" %%i in ('dir /b "*.proto"') do (

    echo %%i to c#  
    ..\protoc.exe --csharp_out=..\out_cs "%%i" 

)

cd ..

copy /y .\out_cs\*.cs ..\Assets\Scripts\Protobuf

echo generate finish!

pause