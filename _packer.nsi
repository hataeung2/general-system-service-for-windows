; to build,
; $ makensis _packer.nsi

; define const
!define DEVELOPER_CONTACT "https://hataeung2.github.io"

!define PKG_NAME "gss-pack"
!define PKG_REVISION "1"
; MAJOR.MINOR.PATCH.BUILT
!define VERSION "1"
!define BRANCH "0"
!define PATCH "0"
!define /date TIMESTAMP "%Y%m%d%H%M"
!define BUILT "${TIMESTAMP}"

!define PRODUCT_VERSION "${VERSION}.${BRANCH}.${PATCH}.${BUILT}"


RequestExecutionLevel admin

!define DEST_DIR "C:\GeneralSystemService"
InstallDir "${DEST_DIR}"

; folders, files to pack (relative path from current directory)
!define OUT_DIR "out"

; after build, 
OutFile "out-packages\GeneralSystemServicePackage_v${PRODUCT_VERSION}_.exe"

; install info
Name "General System Service Installer"
SetCompressor zlib
ShowInstDetails show

; file descriptions
VIProductVersion "${PRODUCT_VERSION}"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "FileDescription" "General System Service Package Installer"
VIAddVersionKey "CompanyName" "atug"
VIAddVersionKey "LegalCopyright" "atug. Inc."

; install prerequisites check
; .NET Framework 4.5.2
; ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\.NETFramework" "InstallRoot"
; StrCpy $hasNetFramwork452 ""
; StrCmp $R0 "" notFound foundIt
; foundIt:
;   IfFileExists "$RO\v4.0.30319\*.*" VersionFound notFound
; VersionFound:
;   StrCpy $hasNetFramwork452 "t"
; notFound:

; service stop and delete
Function UninstallPreviousServicePackage
  DetailPrint "Stopping the service..."
  ExecWait "net stop gss"
  DetailPrint "Deleting the service..."
  ExecWait "installutil /u C:\GeneralSystemService\general_system_service.exe"
FunctionEnd

Function StopWatchdogProcess
  DetailPrint "Stopping the Watchdog process..."
  nsExec::Exec "TaskKill /f /IM awatchdog.exe"
FunctionEnd
Section "Prepare" S01
  Call UninstallPreviousServicePackage
  Call StopWatchdogProcess
SectionEnd

Section "Remove previous service package" S02
MessageBox MB_YESNO "Overwriting 'GeneralSystemService' directory. Are you sure?" IDYES true IDNO false
  false:
    Abort
  true:
    ; next section
SectionEnd


Section "Install" S03
  SetOutPath "$INSTDIR"
  File "${OUT_DIR}\general_system_service.exe"
  File "${OUT_DIR}\batch\gss-install.bat"
  File "${OUT_DIR}\batch\gss-start.bat"
  File "${OUT_DIR}\batch\gss-stop.bat"
  File "${OUT_DIR}\batch\gss-uninstall.bat"
  File "${OUT_DIR}\awatchdog.db"
  File "${OUT_DIR}\awatchdog.exe"
  File "${OUT_DIR}\awatchdog.ico"
  File "${OUT_DIR}\EntityFramework.dll"
  File "${OUT_DIR}\EntityFramework.SqlServer.dll"
  File "${OUT_DIR}\System.Data.SQLite.dll"
  File "${OUT_DIR}\System.Data.SQLite.EF6.dll"
  File "${OUT_DIR}\System.Data.SQLite.Linq.dll"
  File /r "${OUT_DIR}\x64"
  File /r "${OUT_DIR}\x86"
  File "license.txt"
  File "release-note.txt"
  File "README.md"
SectionEnd


; service install and start
Function InstallServicePackage
  DetailPrint "Installing the service..."
  ExecWait "installutil C:\GeneralSystemService\general_system_service.exe"
FunctionEnd
Section "Service Install and start" S04
  Call InstallServicePackage
  MessageBox MB_YESNO "Do you want to start the watchdog service right now?" IDYES true IDNO false
  true:
    DetailPrint "Starting the service..."
    Exec "net start gss"
  false:
    ; done without start  
SectionEnd