;-----------------------------------------------------------
; Inno Setup Script for MedvanaPrintAgent
;-----------------------------------------------------------

#define SingleExe "C:\Users\itzel\Documents\GitHub\MedvanaPrintAgent\bin\Release\net8.0\win-x64\publish\MedvanaPrintAgent.exe"
#define ConfigTemplate "C:\Users\itzel\Documents\GitHub\MedvanaPrintAgent\config\printer_agent.properties"
#define MyAppIcon "C:\Users\itzel\Documents\GitHub\MedvanaPrintAgent\assets\PrintAgentIcon.ico"


[Setup]
AppName=MedvanaPrintAgent
AppVersion=1.0.0
DefaultDirName={autopf}\MedvanaPrintAgent
DefaultGroupName=MedvanaPrintAgent
OutputBaseFilename=MedvanaPrintAgentInstaller
Compression=lzma
SolidCompression=yes
SetupIconFile={#MyAppIcon}

[Files]
Source: "{#SingleExe}";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#ConfigTemplate}";      DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MedvanaPrintAgent"; Filename: "{app}\MedvanaPrintAgent.exe"; IconFilename: "{#MyAppIcon}"

[Run]
Filename: "sc.exe"; Parameters: "create MedvanaPrintAgentService binPath= ""{app}\MedvanaPrintAgent.exe"" start= auto DisplayName= ""Medvana Print Agent"""; \
    StatusMsg: "Registering service..."; Flags: runhidden
Filename: "sc.exe"; Parameters: "start MedvanaPrintAgentService"; \
    StatusMsg: "Starting service..."; Flags: runhidden

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

