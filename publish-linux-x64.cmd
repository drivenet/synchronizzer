@echo off
rmdir /s /q packages\linux-x64\gridfs-sync-service
mkdir packages\linux-x64\gridfs-sync-service
dotnet publish gridfs-sync-service --output ..\packages\linux-x64\gridfs-sync-service -c Release -r linux-x64 --self-contained false
rmdir /s /q packages\linux-x64\gridfs-sync-service\refs
del packages\linux-x64\gridfs-sync-service\gridfs-sync-service packages\linux-x64\gridfs-sync-service\web.config packages\linux-x64\gridfs-sync-service\*.deps.json
