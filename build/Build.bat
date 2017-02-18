SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.MSBuild.MaxVersion=14.0
SET Arbor.X.Tools.External.VisualStudio.Version.PreRelease.Enabled=false
SET Arbor.X.Tools.External.VisualStudio.Version=14.0
SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true

CALL "%~dp0Build.exe"

EXIT /B %ERRORLEVEL%