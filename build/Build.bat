SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.VisualStudio.Version.PreRelease.Enabled=false
SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true

CALL "%~dp0Build.exe"

EXIT /B %ERRORLEVEL%