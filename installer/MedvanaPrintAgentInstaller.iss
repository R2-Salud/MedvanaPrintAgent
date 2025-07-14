;-----------------------------------------------------------
; Inno Setup Script for MedvanaPrintAgent
;-----------------------------------------------------------

#define SingleExe "C:\Users\rtapi\OneDrive\Documentos\GitHub\MedvanaPrintAgent\bin\Release\net8.0\win-x64\publish\MedvanaPrintAgent.exe"
#define MonitorExe "C:\Users\rtapi\OneDrive\Documentos\GitHub\MedvanaPrintAgent\bin\Release\net8.0\win-x64\publish\MedvanaPrintAgentMonitor\MedvanaPrintAgentMonitor.exe"
#define ConfigTemplate "C:\Users\rtapi\OneDrive\Documentos\GitHub\MedvanaPrintAgent\config\printer_agent.properties"
#define MyAppIcon "C:\Users\rtapi\OneDrive\Documentos\GitHub\MedvanaPrintAgent\assets\PrintAgentIcon.ico"


[Setup]
AppName=MedvanaPrintAgent
AppPublisher=Medvana
AppVersion=1.0.3

DefaultDirName={autopf}\MedvanaPrintAgent
DefaultGroupName=MedvanaPrintAgent
OutputBaseFilename=MedvanaPrintAgentInstaller
Compression=lzma
SolidCompression=yes
SetupIconFile={#MyAppIcon}

[Files]
Source: "{#SingleExe}";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#MonitorExe}";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#ConfigTemplate}";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppIcon}";           DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MedvanaPrintAgent"; Filename: "{app}\MedvanaPrintAgent.exe"; IconFilename: "{#MyAppIcon}"

[Run]
Filename: "sc.exe"; Parameters: "create MedvanaPrintAgentService binPath= ""{app}\MedvanaPrintAgent.exe"" start= delayed-auto DisplayName= ""Medvana Print Agent"""; \
    StatusMsg: "Registering main service..."; Flags: runhidden
Filename: "sc.exe"; Parameters: "start MedvanaPrintAgentService"; \
    StatusMsg: "Starting main service..."; Flags: runhidden

Filename: "sc.exe"; Parameters: "create MedvanaPrintAgentMonitorService binPath= ""{app}\MedvanaPrintAgentMonitor.exe"" start= delayed-auto DisplayName= ""Medvana Print Agent Monitor"""; \
    StatusMsg: "Registering monitor service..."; Flags: runhidden
Filename: "sc.exe"; Parameters: "start MedvanaPrintAgentMonitorService"; \
    StatusMsg: "Starting monitor service..."; Flags: runhidden

[Code]
//------------------------------------------------------------------------------
// Return all subkey names under a given registry key.
// e.g. list of printers under HKLM\SYSTEM\CurrentControlSet\Control\Print\Printers
//------------------------------------------------------------------------------
function GetPrinterList: TArrayOfString;
begin
  // On Windows 7 and later, installed printers are under this key:
  RegGetSubkeyNames(
    HKLM,
    'SYSTEM\CurrentControlSet\Control\Print\Printers',
    Result
  );
end;

var
  PrinterPage: TWizardPage;
  PrinterCombo: TComboBox;

//------------------------------------------------------------------------------
// Create a custom wizard page with a combo box of detected printers
//------------------------------------------------------------------------------
procedure InitializeWizard;
var
  list: TArrayOfString;
  i: Integer;
begin
  PrinterPage := CreateCustomPage(
    wpSelectDir,
    'Select Printer',
    'Please choose your Zebra printer:'
  );

  PrinterCombo := TComboBox.Create(PrinterPage.Surface);
  PrinterCombo.Parent := PrinterPage.Surface;
  PrinterCombo.Left  := ScaleX(0);
  PrinterCombo.Top   := ScaleY(8);
  PrinterCombo.Width := ScaleX(300);

  list := GetPrinterList;
  if GetArrayLength(list) > 0 then
  begin
    for i := 0 to GetArrayLength(list)-1 do
      PrinterCombo.Items.Add(list[i]);
    PrinterCombo.ItemIndex := 0;
  end
  else
  begin
    PrinterCombo.Items.Add('No printers found');
    PrinterCombo.ItemIndex := 0;
  end;
end;

//------------------------------------------------------------------------------
// After install, write the selected printer name into printer_agent.properties
//------------------------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
var
  selected, content: String;
begin
  if CurStep = ssPostInstall then
  begin
    if Assigned(PrinterCombo) then
      selected := PrinterCombo.Text
    else
      selected := '';
    content := 'printer.name=' + selected;
    if not SaveStringToFile(
         ExpandConstant('{app}\printer_agent.properties'),
         content,
         False
       ) then
    begin
      MsgBox('Error: could not write printer_agent.properties', mbError, MB_OK);
    end;
  end;
end;

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting('AppName')}"; \
    ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\PrintAgentIcon.ico"; Flags: uninsdeletekey

