; ============================================================================
;  LIKAsys - installer
;  Made in Kosovo with love | Likaapps.com
;  Compiled with NSIS 3 (makensis LIKAsys.nsi)
; ============================================================================

Unicode true

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"

!ifndef VERSION
  !define VERSION "1.0"
!endif
!ifndef VERSION4
  !define VERSION4 "1.0.0.0"
!endif

!define APPNAME     "LIKAsys"
!define COMPANY     "LIKAsys"
!define EXEFILE     "LIKAsys.exe"
!define TASKNAME    "LIKAsys"
!define WEBSITE     "https://Likaapps.com"
!define REGKEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\LIKAsys"
!define APPREG      "Software\LIKAsys"

Name "${APPNAME} ${VERSION}"
OutFile "..\dist\LIKAsys-Setup-${VERSION}.exe"
InstallDir "$PROGRAMFILES64\${APPNAME}"
InstallDirRegKey HKLM "${APPREG}" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
SetCompressorDictSize 64
ShowInstDetails show
ShowUnInstDetails show
BrandingText "${APPNAME} ${VERSION} - Made in Kosovo"

VIProductVersion "${VERSION4}"
VIAddVersionKey "ProductName"     "${APPNAME}"
VIAddVersionKey "CompanyName"     "${COMPANY}"
VIAddVersionKey "FileDescription" "LIKAsys Setup - monitor i sistemit (CPU, GPU, RAM, FPS)"
VIAddVersionKey "FileVersion"     "${VERSION4}"
VIAddVersionKey "ProductVersion"  "${VERSION}"
VIAddVersionKey "LegalCopyright"  "(c) 2026 LIKAsys"

Var UpdateMode

; ---------------------------------------------------------------- interface
!define MUI_ABORTWARNING
!define MUI_ICON   "..\src\LIKAsys\assets\LIKAsys.ico"
!define MUI_UNICON "..\src\LIKAsys\assets\LIKAsys.ico"

!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "header.bmp"
!define MUI_HEADERIMAGE_RIGHT
!define MUI_WELCOMEFINISHPAGE_BITMAP "wizard.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "wizard.bmp"

!define MUI_WELCOMEPAGE_TITLE "LIKAsys ${VERSION}"
!define MUI_WELCOMEPAGE_TEXT "LIKAsys eshte monitor i lehte i sistemit per gamer-a.$\r$\n$\r$\nTregon CPU, GPU, VRAM, RAM dhe FPS ne nje kartele te vogel e elegante ne qoshe te ekranit. Rri gjithmone siper lojes dhe fshihet ne ikonen afer ores.$\r$\n$\r$\nFalas pergjithmone. Pa reklama. Pa pagesa.$\r$\n$\r$\nKliko 'Next' per te vazhduar."

!define MUI_DIRECTORYPAGE_TEXT_TOP "LIKAsys do te instalohet ne dosjen e meposhtme. Per nje vend tjeter kliko 'Browse'."

!define MUI_FINISHPAGE_TITLE "LIKAsys u instalua"
!define MUI_FINISHPAGE_TEXT "Gjithcka u krye. Widget-i shfaqet ne qoshe te ekranit dhe ikona e tij rri afer ores.$\r$\n$\r$\nFaleminderit qe perdor LIKAsys - Made in Kosovo."
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXEFILE}"
!define MUI_FINISHPAGE_RUN_TEXT "Hap LIKAsys tani"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Nis bashke me Windows"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION EnableAutoStart
!define MUI_FINISHPAGE_LINK "Likaapps.com"
!define MUI_FINISHPAGE_LINK_LOCATION "${WEBSITE}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Albanian"
!insertmacro MUI_LANGUAGE "English"

; ---------------------------------------------------------------- helpers
Function StopRunning
  DetailPrint "Po mbyllet LIKAsys nese eshte hapur..."
  ; NOTE: never use /T here. When the update is launched from inside the app this
  ; installer is a CHILD of LIKAsys.exe, and /T would kill the installer itself.
  nsExec::Exec 'taskkill /IM "${EXEFILE}"'
  Pop $0
  Sleep 700
  nsExec::Exec 'taskkill /F /IM "${EXEFILE}"'
  Pop $0
  Sleep 1800
FunctionEnd

Function EnableAutoStart
  nsExec::ExecToLog 'schtasks /Create /TN "${TASKNAME}" /TR "$\"$INSTDIR\${EXEFILE}$\" --autostart" /SC ONLOGON /RL HIGHEST /F'
  Pop $0
FunctionEnd

Function .onInit
  ${IfNot} ${AtLeastWin8}
    MessageBox MB_OK|MB_ICONSTOP "LIKAsys kerkon Windows 10 ose 11."
    Abort
  ${EndIf}

  StrCpy $UpdateMode "0"
  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "/UPDATE" $R1
  ${IfNot} ${Errors}
    StrCpy $UpdateMode "1"
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------- install
Section "LIKAsys" SecMain
  SectionIn RO
  SetShellVarContext all

  Call StopRunning

  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "..\build\app\*.*"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${EXEFILE}" "" "$INSTDIR\${EXEFILE}" 0
  CreateShortCut "$SMPROGRAMS\${APPNAME}\Cinstalo ${APPNAME}.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${EXEFILE}" "" "$INSTDIR\${EXEFILE}" 0

  WriteRegStr HKLM "${APPREG}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${APPREG}" "Version" "${VERSION}"

  WriteRegStr   HKLM "${REGKEY}" "DisplayName"     "${APPNAME} - monitor i sistemit"
  WriteRegStr   HKLM "${REGKEY}" "DisplayIcon"     "$INSTDIR\${EXEFILE}"
  WriteRegStr   HKLM "${REGKEY}" "DisplayVersion"  "${VERSION}"
  WriteRegStr   HKLM "${REGKEY}" "Publisher"       "${COMPANY}"
  WriteRegStr   HKLM "${REGKEY}" "URLInfoAbout"    "${WEBSITE}"
  WriteRegStr   HKLM "${REGKEY}" "HelpLink"        "${WEBSITE}"
  WriteRegStr   HKLM "${REGKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr   HKLM "${REGKEY}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegStr   HKLM "${REGKEY}" "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
  WriteRegDWORD HKLM "${REGKEY}" "NoModify" 1
  WriteRegDWORD HKLM "${REGKEY}" "NoRepair" 1

  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${REGKEY}" "EstimatedSize" "$0"
SectionEnd

Function .onInstSuccess
  ${If} ${Silent}
    ; silent update: give the old process time to release the single-instance
    ; mutex, then bring the widget straight back
    Sleep 1200
    Exec '"$INSTDIR\${EXEFILE}" --updated'
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------- uninstall
Section "Uninstall"
  SetShellVarContext all

  nsExec::Exec 'taskkill /F /IM "${EXEFILE}"'
  Pop $0
  Sleep 1200

  nsExec::Exec 'schtasks /Delete /TN "${TASKNAME}" /F'
  Pop $0

  Delete "$DESKTOP\${APPNAME}.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\Cinstalo ${APPNAME}.lnk"
  RMDir  "$SMPROGRAMS\${APPNAME}"

  RMDir /r "$INSTDIR"

  DeleteRegKey HKLM "${REGKEY}"
  DeleteRegKey HKLM "${APPREG}"
SectionEnd
