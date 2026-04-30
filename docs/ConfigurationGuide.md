# Hospital Queueing System Configuration Guide

This project has two apps:

- `MultiDepartmentQueueing`: public display/dashboard.
- `HospitalQueueCaller`: department counter app for issuing, calling, repeating, and skipping numbers.

Each app reads `appsettings.json` from its own application folder. Keep shared settings aligned in both apps when deploying more than one executable.

## Logos and Display Text

Display app:

```json
"Logos": {
  "MainLogoPath": "assets/logos/MedixHMSFullLogo.png",
  "SecondaryLogoPath": "assets/logos/SKPH Hospital Logo.png",
  "MultiDepartmentQueueingIconPath": "assets/icons/MultiDepartmentQueueing.png",
  "HospitalQueueCallerIconPath": "assets/icons/MedixHMSIcon.png"
},
"DisplayText": {
  "WindowTitle": "Hospital Queueing Display",
  "HeaderTitle": "Sultan Kudarat Provincial Hospital Queuing System",
  "NowServingTitle": "Now Serving"
}
```

Caller app:

```json
"Logos": {
  "MainLogoPath": "assets/logos/MedixHMSFullLogo.png",
  "SecondaryLogoPath": "",
  "MultiDepartmentQueueingIconPath": "",
  "HospitalQueueCallerIconPath": "assets/icons/MedixHMSIcon.png"
}
```

- `MainLogoPath`: Medix HMS logo used by the apps.
- `SecondaryLogoPath`: hospital logo shown on the display app when configured.
- Icon paths are used for Windows app icons.
- Relative paths are resolved from each app folder.

## Queue Storage

```json
"QueueStorage": {
  "StateFilePath": "%LOCALAPPDATA%/HospitalQueueingSystem/hospital_queue_state.json"
}
```

This is the most important sync setting.

- Same PC: use the same local path in all apps.
- Multiple PCs: use one network shared path in all apps.

Example network path:

```json
"StateFilePath": "//192.168.1.200/HospitalQueueData/hospital_queue_state.json"
```

Do not store Windows usernames or passwords in the app. Use Windows folder sharing and permissions.

## Departments

Both apps currently have the same 17 configured departments.

```json
"Departments": [
  { "Name": "Dental", "Code": "DEN", "AccentColor": "#2563EB" }
]
```

- `Name`: shown on display and caller.
- `Code`: appears in ticket numbers, for example `DEN-R001`.
- `AccentColor`: department color used in the UI.

Configured departments:

| Department | Code |
| --- | --- |
| Dental | DEN |
| Animal Bite | AB |
| Family Planning | FP |
| Ophthalmology | OPT |
| OB-Gyne | OBG |
| Surgery | SUR |
| Orthopedic | ORT |
| Urology | URO |
| Family Medicine | FM |
| Pediatrics | PED |
| Internal Medicine - Cardiology | IMC |
| Internal Medicine - Neurology | NEU |
| Internal Medicine - Nephrology | NEP |
| Internal Medicine - Pulmonology | PUL |
| Internal Medicine - Gastroenterology | GAS |
| Internal Medicine - Oncology | ONC |
| FAMED-AnimalBite | FAB |

## Authentication

Caller users are stored in:

```json
"Authentication": {
  "UsersFilePath": "config/users.txt"
}
```

Format:

```text
username|password|departmentCode|displayName
```

The `departmentCode` must match one configured department code. The current file has one user for each configured department:

```text
dental|1234|DEN|Dental
animalbite|1234|AB|Animal Bite
familyplanning|1234|FP|Family Planning
opthalmology|1234|OPT|Ophthalmology
obgyne|1234|OBG|OB-Gyne
surgery|1234|SUR|Surgery
orthopedic|1234|ORT|Orthopedic
urology|1234|URO|Urology
familymedicine|1234|FM|Family Medicine
pediatrics|1234|PED|Pediatrics
cardiology|1234|IMC|Internal Medicine - Cardiology
neurology|1234|NEU|Internal Medicine - Neurology
nephrology|1234|NEP|Internal Medicine - Nephrology
pulmonology|1234|PUL|Internal Medicine - Pulmonology
gastroenterology|1234|GAS|Internal Medicine - Gastroenterology
oncology|1234|ONC|Internal Medicine - Oncology
famedanimalbite|1234|FAB|FAMED-AnimalBite
```

## Print

```json
"Print": {
  "PrinterMode": "Default",
  "PrinterName": "",
  "TicketTempFolder": "%TEMP%/HospitalQueueTickets",
  "PaperName": "Queue Ticket 58x90mm",
  "PaperWidthHundredthsInch": 228,
  "PaperHeightHundredthsInch": 354
}
```

- `PrinterMode`: `Default` or `Named`.
- `PrinterName`: installed printer name when using `Named`.
- `TicketTempFolder`: every issued ticket is saved here before printing.
- Paper values are in hundredths of an inch. `228 x 354` is about `58mm x 90mm`.

## Speech

Display app speech is enabled; caller app speech is disabled.

```json
"Speech": {
  "Enabled": true,
  "VoiceName": "Microsoft David Desktop",
  "Rate": 0,
  "Volume": 100,
  "RepeatCount": 1,
  "RepeatDelayMilliseconds": 1200,
  "AnnouncementFormat": "Now serving number {Number}. Please proceed to {Counter}, {Department}.",
  "PronunciationOverrides": {
    "AB": "ey bi",
    "AP": "ey bi"
  }
}
```

- Uses installed Windows voices.
- Leave `VoiceName` blank to use the default Windows voice.
- Supported placeholders: `{Number}`, `{Counter}`, `{Department}`, `{Lane}`.
- `PronunciationOverrides` helps speech pronounce department codes clearly.

## UI Theme

`UI.Theme` can be `Dark` or `Light`.

- Display app default: `Dark`.
- Caller app default: `Light`.

Colors are semantic:

- `BackgroundColor`
- `SurfaceColor`
- `CardColor`
- `PrimaryTextColor`
- `SecondaryTextColor`
- `CardTextColor`
- `CardSecondaryTextColor`
- `AccentColor`
- `PriorityColor`
- `DangerColor`
- `SuccessColor`

Keep high contrast for readability, especially for elderly users.

## Daily Reset

Queue issuance resets automatically each day:

- Regular numbers return to `001`.
- Priority numbers return to `001`.
- Waiting tickets and recent calls are cleared.

## Verify Build

Use these commands from the repository root to create fresh verification outputs:

```powershell
$env:DOTNET_CLI_HOME='C:\Users\Ver\OneDrive\Documents\QueueuingNoIssuer\.dotnet_home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build .\HospitalQueueCaller\HospitalQueueCaller.csproj -o .\verify-build\caller
dotnet build .\MultiDepartmentQueueing\MultiDepartmentQueueing.csproj -o .\verify-build\display
```

Expected outputs:

- `verify-build/caller/HospitalQueueCaller.exe`
- `verify-build/display/MultiDepartmentQueueing.exe`

## Recommended Deployment

1. Install `MultiDepartmentQueueing` on the dashboard PC.
2. Install `HospitalQueueCaller` on each counter PC, or run it on the same PC.
3. Create a shared folder, for example `C:\HospitalQueueData`.
4. Share it as `HospitalQueueData`.
5. Give caller PCs read/write permission.
6. Set all apps to:

```json
"StateFilePath": "//192.168.1.200/HospitalQueueData/hospital_queue_state.json"
```

7. Use Windows Credential Manager or domain/local accounts for access.
