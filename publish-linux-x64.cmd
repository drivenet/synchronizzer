@echo off
rmdir /s /q packages\linux-x64\synchronizzer
mkdir packages\linux-x64\synchronizzer
dotnet publish synchronizzer --output packages\linux-x64\synchronizzer -c Release -r linux-x64 --self-contained false
del packages\linux-x64\synchronizzer\synchronizzer packages\linux-x64\synchronizzer\web.config packages\linux-x64\synchronizzer\*.deps.json
