@echo off
rmdir /s /q packages\linux-x64\synchronizzer
mkdir packages\linux-x64\synchronizzer
dotnet publish Synchronizzer --force --output packages\linux-x64\synchronizzer -c Integration -r linux-x64 --no-self-contained
del packages\linux-x64\synchronizzer\*.deps.json
rmdir /s /q packages\linux-x64\synchronizzer-single
mkdir packages\linux-x64\synchronizzer-single
dotnet publish Synchronizzer --force --output packages\linux-x64\synchronizzer-single -c Integration -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
