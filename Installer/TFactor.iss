; TFactor installer script (Inno Setup 6.3+: https://jrsoftware.org/isinfo.php)
;
; Build steps:
;   1. Publish the app first:
;        dotnet publish ..\TFactor\TFactor.csproj -c Release -p:PublishProfile=SelfContained
;      This produces a single self-contained TFactor.exe (no companion DLLs) at the PublishDir below.
;   2. Compile this script:
;        iscc TFactor.iss
;      Output: Output\TFactorSetup.exe
;
; Bump MyAppVersion below before cutting a new release - AppId must stay the same across versions so upgrades
; install over the previous copy instead of side-by-side.

#define MyAppName "TFactor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Tom Oddy"
#define MyAppExeName "TFactor.exe"
#define MyAppId "{{3C8018D3-36E3-4D06-9E5C-5F40DDA89BDC}"
#define PublishDir "..\TFactor\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Per-user install, no admin/UAC prompt - matches the app's secrets already being tied to the current Windows user via DPAPI
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=TFactorSetup
SetupIconFile=..\TFactor\Icons\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
