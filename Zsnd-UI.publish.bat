for %%a in (x86, x64) do (
    dotnet publish "Zsnd-UI.csproj" -r win-%%a -p:Platform=%%a -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:PublishDir=bin\Release\net10.0\win-%%a\publish\
)
rem --self-contained true -p:PublishSingleFile=false are defined in profile
rem trimming not working (might not be needed)
rem -p:PublishTrimmed=true -p:TrimMode=partial
rem single file seems to use trimming, which might be why that fails, too.