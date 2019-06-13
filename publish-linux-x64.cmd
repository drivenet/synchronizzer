@echo off
rmdir /s /q packages\gridfs-sync-service-linux-x64
mkdir packages\gridfs-sync-service-linux-x64
dotnet publish gridfs-sync-service --output ..\packages\gridfs-sync-service-linux-x64 -c Release -r linux-x64 --self-contained false
rmdir /s /q packages\gridfs-sync-service-linux-x64\refs
del packages\gridfs-sync-service-linux-x64\gridfs-sync-service packages\gridfs-sync-service-linux-x64\web.config packages\gridfs-sync-service-linux-x64\*.deps.json
