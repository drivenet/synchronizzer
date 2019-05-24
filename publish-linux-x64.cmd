@echo off
rmdir /s /q gridfs-sync-service\bin\x64\Release\netcoreapp2.2\linux-x64\publish
dotnet publish -c Release -r linux-x64 --self-contained false
rmdir /s /q packages\gridfs-sync-service-linux-x64
mkdir packages\gridfs-sync-service-linux-x64
xcopy gridfs-sync-service\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.dll packages\gridfs-sync-service-linux-x64
xcopy gridfs-sync-service\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.pdb packages\gridfs-sync-service-linux-x64
xcopy gridfs-sync-service\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.so packages\gridfs-sync-service-linux-x64
xcopy gridfs-sync-service\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.runtimeconfig.json packages\gridfs-sync-service-linux-x64
