# Hospital Queueing System Configuration Guide

This project has three apps:

- `MultiDepartmentQueueing`: public display/dashboard.
- `HospitalQueueCaller`: department counter app for calling, repeating, and skipping numbers.
- `HospitalQueueIssuer`: ticket issuer/kiosk app for issuing and printing queue numbers.

All apps use `appsettings.json` in their own application folder.

## Logos

```json
"Logos": {
  "MainLogoPath": "assets/logos/MedixHMSFullLogo.png",
  "SecondaryLogoPath": "assets/logos/HospitalLogo.png",
  "MultiDepartmentQueueingIconPath": "assets/icons/MultiDepartmentQueueing.png",
  "HospitalQueueCallerIconPath": "assets/icons/MedixHMSIcon.png"
}
```

- `MainLogoPath`: top-left logo used by apps.
- `SecondaryLogoPath`: top-right display logo.
- Icon paths are used for Windows app icons.
- Relative paths are resolved from the app folder.

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

Do not store Windows usernames/passwords in the app. Use Windows folder sharing and permissions.

## Departments

```json
"Departments": [
  { "Name": "Emergency", "Code": "EMR", "AccentColor": "#DC2626" }
]
```

- `Name`: shown on display, caller, and issuer.
- `Code`: appears in ticket numbers, e.g. `EMR-R001`.
- `AccentColor`: department color.

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

Example:

```text
emergency|1234|EMR|Emergency
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

```json
"Speech": {
  "Enabled": true,
  "VoiceName": "",
  "Rate": 0,
  "Volume": 100,
  "RepeatCount": 1,
  "RepeatDelayMilliseconds": 1200,
  "AnnouncementFormat": "Now serving number {Number}. Please proceed to {Counter}, {Department}."
}
```

- Uses free installed Windows voices.
- Leave `VoiceName` blank to use default Windows voice.
- Supported placeholders: `{Number}`, `{Counter}`, `{Department}`, `{Lane}`.

## UI Theme

`UI.Theme` can be `Dark` or `Light`. Colors are semantic:

- `BackgroundColor`
- `SurfaceColor`
- `CardColor`
- `PrimaryTextColor`
- `SecondaryTextColor`
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

## Recommended Deployment

1. Install/display `MultiDepartmentQueueing` on the dashboard PC.
2. Create a shared folder, for example `C:\HospitalQueueData`.
3. Share it as `HospitalQueueData`.
4. Give caller/issuer PCs read/write permission.
5. Set all apps to:

```json
"StateFilePath": "//192.168.1.200/HospitalQueueData/hospital_queue_state.json"
```

6. Use Windows Credential Manager or domain/local accounts for access.
