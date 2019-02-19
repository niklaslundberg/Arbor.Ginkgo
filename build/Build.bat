@ECHO OFF

SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.VisualStudio.Version.PreRelease.Enabled=false
SET Arbor.X.Tools.External.NuGet.Restore.Enabled=true

CALL dotnet arbor-build

EXIT /B %ERRORLEVEL%