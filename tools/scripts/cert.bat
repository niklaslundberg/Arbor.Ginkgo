@ECHO OFF

SETLOCAL enabledelayedexpansion
SET makecertexe=C:\Program Files (x86)\Windows Kits\10\bin\x64\makecert.exe
SET pfxexe=C:\Program Files (x86)\Windows Kits\10\bin\x64\pvk2pfx.exe

SET WorkingDirectory=%~dp0
SET CertificateName=iisexpresstestlocal
SET CertficateOutputFile=%WorkingDirectory%%CertificateName%.cer
SET CertficateKeyFile=%WorkingDirectory%%CertificateName%.pvk
SET PfxFile=%WorkingDirectory%%CertificateName%.pfx
SET Subject=iisexpresstest.local
SET SubjectCN=CN=%Subject%
SET password=1234
SET ValidTo=12/31/2030

IF EXIST "!makecertexe!" (
  ECHO "!makecertexe!" exists
) ELSE (
  ECHO "!makecertexe!" does not exist
  GOTO END
)

IF EXIST "!pfxexe!" (
  ECHO "!pfxexe!" exists
) ELSE (
  ECHO "!pfxexe!" does not exist
  GOTO END
)

IF EXIST "%CertficateOutputFile%" (
  ECHO Deleting existing file %CertficateOutputFile%
  DEL "%CertficateOutputFile%"
)

IF EXIST "%CertficateKeyFile%" (
  ECHO Deleting existing file %CertficateKeyFile%
  DEL "%CertficateKeyFile%"
)

IF EXIST "%PfxFile%" (
  ECHO Deleting existing file %PfxFile%
  DEL "%PfxFile%"
)

ECHO WorkingDirectory = %WorkingDirectory%
ECHO CertificateName = %CertificateName%
ECHO CertficateOutputFile = %CertficateOutputFile%
ECHO CertficateKeyFile = %CertficateKeyFile%
ECHO PfxFile = %PfxFile%
ECHO Subject = %Subject%
ECHO SubjectCN = %SubjectCN%
ECHO password = %password%
ECHO ValidTo = %ValidTo%

CALL "!makecertexe!" -n "%SubjectCN%" -pe -a sha256 -e "%ValidTo%" -sky signature -r -sv "%CertficateKeyFile%" "%CertficateOutputFile%"
 
CALL "!pfxexe!" -pvk "%CertficateKeyFile%" -spc "%CertficateOutputFile%" -pfx "%PfxFile%" -pi "%password%" -po "%password%" -f

:END