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
!include "nsDialogs.nsh"

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
Var UserProfile
Var UserProfileDlg
Var RbGaming
Var RbIt

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
Page custom ProfilePageShow ProfilePageLeave
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Albanian"
!insertmacro MUI_LANGUAGE "English"


; ---------------------------------------------------------------- profile page
; Asked once, at install time: is this machine for gaming or for IT work?
; The answer lands in the registry and LIKAsys reads it the first time it runs.
; Skipped entirely during an in-app update - nobody wants to answer this twice.
Function ProfilePageShow
  ${If} $UpdateMode == "1"
    Abort
  ${EndIf}

  !insertmacro MUI_HEADER_TEXT "Per cfare e perdor kete kompjuter?" "LIKAsys pershtatet vete sipas pergjigjes."

  nsDialogs::Create 1018
  Pop $UserProfileDlg
  ${If} $UserProfileDlg == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0 0 100% 26u "Zgjidh nje profil. Ai vendos cilat matje shfaqen, si duken ikonat dhe cfare ngjyre ka widget-i. Mund ta ndryshosh kurdo me vone te Cilesimet."
  Pop $0

  ; both radio buttons are created back to back so Windows treats them as one
  ; group, and an explicit click handler enforces it even if it does not
  ${NSD_CreateRadioButton} 0 34u 100% 12u "Gaming"
  Pop $RbGaming
  ${NSD_CreateRadioButton} 0 80u 100% 12u "IT"
  Pop $RbIt

  ${NSD_CreateLabel} 14u 47u 95% 28u "FPS, 1% low, VRAM dhe temperatura. Numra te medhenj, ikona 3D, theks cyan. Zgjidhe kete nese luan."
  Pop $0
  ${NSD_CreateLabel} 14u 93u 95% 28u "Disku, lexim/shkrim, rrjeti, ping dhe uptime. Rreshta te ngjeshur, ikona teknike, theks i gjelber. Zgjidhe kete nese punon me kompjutera."
  Pop $0

  ${NSD_OnClick} $RbGaming OnPickGaming
  ${NSD_OnClick} $RbIt OnPickIt

  ${If} $UserProfile == "it"
    ${NSD_Check} $RbIt
  ${Else}
    ${NSD_Check} $RbGaming
  ${EndIf}

  nsDialogs::Show
FunctionEnd

Function OnPickGaming
  ${NSD_Check} $RbGaming
  ${NSD_Uncheck} $RbIt
FunctionEnd

Function OnPickIt
  ${NSD_Check} $RbIt
  ${NSD_Uncheck} $RbGaming
FunctionEnd

Function ProfilePageLeave
  ${NSD_GetState} $RbIt $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $UserProfile "it"
  ${Else}
    StrCpy $UserProfile "gaming"
  ${EndIf}
FunctionEnd

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
  StrCpy $UserProfile "gaming"
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

  ; only stamp the profile on a real install; an update must not reset it
  ${If} $UpdateMode != "1"
    WriteRegStr HKLM "${APPREG}" "SetupProfile" "$UserProfile"
  ${EndIf}

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
