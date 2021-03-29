@echo off
rmdir /s /q packages\linux-x64\synchronizzer
mkdir packages\linux-x64\synchronizzer
dotnet publish Synchronizzer --output packages\linux-x64\synchronizzer -c Integration -r linux-x64 --self-contained false
move packages\linux-x64\synchronizzer\Microsoft.Extensions.Hosting.Systemd.dll "%TEMP%"
move packages\linux-x64\synchronizzer\Microsoft.IO.RecyclableMemoryStream.dll "%TEMP%"
del packages\linux-x64\synchronizzer\web.config packages\linux-x64\synchronizzer\*.deps.json packages\linux-x64\synchronizzer\Microsoft.*.dll
move "%TEMP%\Microsoft.Extensions.Hosting.Systemd.dll" packages\linux-x64\synchronizzer
move "%TEMP%\Microsoft.IO.RecyclableMemoryStream.dll" packages\linux-x64\synchronizzer
