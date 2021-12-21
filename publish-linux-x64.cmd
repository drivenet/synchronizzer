@echo off
rmdir /s /q packages\linux-x64\synchronizzer
mkdir packages\linux-x64\synchronizzer
dotnet publish Synchronizzer --force --output packages\linux-x64\synchronizzer -c Integration -r linux-x64 --self-contained false
del packages\linux-x64\synchronizzer\web.config packages\linux-x64\synchronizzer\*.deps.json
