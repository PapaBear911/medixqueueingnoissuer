# Hospital Queueing System Configuration Guide

Documentation Made by: Ver Sangil

This project has three apps:

- `MultiDepartmentQueueing`: public display/dashboard.
- `HospitalQueueCaller`: department counter app for issuing, calling, repeating, and skipping numbers.
- `HospitalQueueAdminSetup`: admin setup tool for pre-configuring display and caller apps before rollout.

Each app reads `appsettings.json` from its own application folder. Keep shared settings aligned in both apps when deploying more than one executable.

## Runtime Requirement

All three apps target `net8.0-windows`, so each Windows PC that will run the Display, Caller, or Admin Setup app must have the .NET 8 Desktop Runtime installed.

Install this on deployment PCs before running the apps:

- Official Microsoft download page: <https://dotnet.microsoft.com/en-US/download/dotnet/8.0>
- Choose `.NET Desktop Runtime 8.0` for Windows.
- Use the `x64` installer for most modern Windows PCs.
- The Desktop Runtime includes the base .NET Runtime, so a separate .NET Runtime install is not needed.

## Admin Setup Application

Use `HospitalQueueAdminSetup` before deployment to centralize configuration for both apps.

The setup app can:

- Select the Display app folder and Caller app folder.
- Configure the queue state path for same-PC or network deployments.
- Add, edit, and remove departments.
- Add, edit, and remove caller users.
- Allow multiple users to share the same department code.
- Save synchronized `Departments` and `QueueStorage.StateFilePath` values into both app `appsettings.json` files.
- Save caller users into `HospitalQueueCaller/config/users.txt`.

Build output:

```powershell
dotnet build .\HospitalQueueAdminSetup\HospitalQueueAdminSetup.csproj -o .\verify-build\admin
```

Run:

```text
verify-build/admin/HospitalQueueAdminSetup.exe
```

Recommended setup flow:

1. Build the Display, Caller, and Admin Setup apps.
2. Open `HospitalQueueAdminSetup`.
3. Select the Display app folder.
4. Select the Caller app folder.
5. Choose deployment mode:
   - Same PC: uses `%LOCALAPPDATA%/HospitalQueueingSystem/hospital_queue_state.json`.
   - Network/shared path: use a shared path such as `//192.168.1.200/HospitalQueueData/hospital_queue_state.json`.
6. Review or edit departments.
7. Review or edit caller users.
   - Use `Remove Selected Department` to remove a department before rollout. Matching users for that department are removed at the same time.
   - Use `Remove Selected User` to remove one caller login before rollout.
8. Click `Validate`.
9. Click `Save and Sync`.
10. Restart the Display and Caller apps so they reload the synchronized configuration.

The setup app does not need a user for every department, but any department that staff will issue or call numbers from needs at least one caller user.

## Logos and Display Text

Display app:

```json
"Logos": {
  "MainLogoPath": "assets/logos/MainLogo.png",
  "SecondaryLogoPath": "assets/logos/SecondaryLogo.png",
  "MultiDepartmentQueueingIconPath": "assets/logos/SecondaryLogo.png",
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
- The display app window icon always uses the configured secondary logo.
- Icon paths are used for Windows app icons when a separate icon path is configured.
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
| Internal Medicine-Cardiology | IMC |
| Internal Medicine-Neurology | NEU |
| Internal Medicine-Nephrology | NEP |
| Internal Medicine-Pulmonology | PUL |
| Internal Medicine-Gastroenterology | GAS |
| Internal Medicine-Oncology | ONC |
| FAMED-AnimalBite | FAB |

## Accent Colors

Use hex colors for department `AccentColor` values. A good source is the Tailwind CSS color palette:

- Tailwind CSS Colors: <https://tailwindcss.com/docs/colors>

For this app, prefer Tailwind `600` or `700` shades. They are strong enough for the dark display UI while still readable on the light caller/admin UI.

Recommended compatible department accent colors:

| Color | Hex | Good Use |
| --- | --- | --- |
| Blue 600 | `#2563EB` | General services, Dental |
| Red 600 | `#DC2626` | Urgent or bite-related services |
| Pink 600 | `#DB2777` | Family Planning, OB-Gyne variants |
| Purple 600 | `#9333EA` | Specialty clinics |
| Violet 600 | `#7C3AED` | Ophthalmology or specialty care |
| Orange 600 | `#EA580C` | Surgery or procedure areas |
| Amber 600 | `#D97706` | Priority-highlight departments |
| Yellow 600 | `#CA8A04` | Orthopedic or support departments |
| Cyan 600 | `#0891B2` | Urology, diagnostics, or navigation-friendly services |
| Teal 600 | `#0D9488` | Pulmonology or calming clinical services |
| Green 600 | `#16A34A` | Pediatrics, cleared/active services |
| Emerald 600 | `#059669` | Family Medicine or general outpatient services |
| Rose 700 | `#BE123C` | Oncology or high-emphasis services |
| Indigo 600 | `#4F46E5` | Neurology or specialty services |
| Sky 600 | `#0284C7` | Nephrology, diagnostics, or information counters |

Avoid very pale colors like `#BFDBFE` or `#FEF3C7` for accents because they can look weak on the dark display. Avoid very dark colors like `#1E293B` because they can blend into the display background.

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
cardiology|1234|IMC|Internal Medicine-Cardiology
neurology|1234|NEU|Internal Medicine-Neurology
nephrology|1234|NEP|Internal Medicine-Nephrology
pulmonology|1234|PUL|Internal Medicine-Pulmonology
gastroenterology|1234|GAS|Internal Medicine-Gastroenterology
oncology|1234|ONC|Internal Medicine-Oncology
famedanimalbite|1234|FAB|FAMED-AnimalBite
```

## Adding Users

Add caller users in `HospitalQueueAdminSetup`. The caller app no longer has a registration screen.

The setup app writes caller users to `HospitalQueueCaller/config/users.txt`.

```text
username|password|departmentCode|displayName
```

Example: two counters can share one department by using the same `departmentCode` with different usernames.

```text
dental1|1234|DEN|Dental Counter 1
dental2|1234|DEN|Dental Counter 2
```

Multiple users can use the same department. They will share the same department queue, number sequence, current call, waiting tickets, and display card because the queue is keyed by department code. Use different usernames when there are multiple counters or staff accounts for the same department.

Synchronization requirements:

- The user only needs to be added in Admin Setup, which saves to the caller app `users.txt`.
- The user's `departmentCode` must already exist in the caller app `Departments` list.
- The same department code should also exist in the display app `Departments` list so the display dashboard shows the department with the correct name and color.
- If caller apps run on multiple PCs, copy the updated `users.txt` to each caller app installation that needs those logins.
- Keep `QueueStorage.StateFilePath` the same on all caller and display apps so every user works against the same queue state.

## Adding a New Department

Preferred workflow:

1. Open `HospitalQueueAdminSetup`.
2. Select the Display app folder and Caller app folder.
3. Add a row in `Departments`.
4. Add at least one matching row in `Caller Users` if staff will issue or call numbers for this department.
5. Click `Validate`.
6. Click `Save and Sync`.
7. Restart the Display and Caller apps.

What this does:

- Adds or updates the department in both app `appsettings.json` files.
- Adds or updates caller users in `HospitalQueueCaller/config/users.txt`.
- Saves the same queue state path into both apps.

Manual configuration pattern:

```json
{ "Name": "New Department", "Code": "NEW", "AccentColor": "#1D4ED8" }
```

```text
newdepartment|1234|NEW|New Department
```

User requirement after adding a department:

- If staff need to log in and issue/call numbers for the new department, at least one caller user is required.
- If the department is display-only and no one will call numbers for it yet, a user is not technically required, but the department will not be actively used until a caller user exists.
- The centralized setup workflow keeps Display and Caller synchronized, so use it for normal operation before rollout.

Synchronization requirements:

- Add the same department object to both `HospitalQueueCaller/appsettings.json` and `MultiDepartmentQueueing/appsettings.json`.
- Use the exact same `Code` in both apps and in `users.txt`.
- Use the same `QueueStorage.StateFilePath` in both apps.
- Rebuild or redeploy both apps after changing source config files, or update the deployed `appsettings.json` files directly if editing installed output folders.
- Restart the display app and caller app after config changes so they reload the new department list.

## Removing a Department

Preferred workflow:

1. Open `HospitalQueueAdminSetup`.
2. Select the department row.
3. Click `Remove Selected Department`.
4. Confirm the removal. Matching caller users are removed at the same time.
5. Click `Validate`.
6. Click `Save and Sync`.
7. Restart the caller app and display app.

Manual fallback:

1. In `HospitalQueueCaller/appsettings.json`, remove the department object from `Departments`.
2. In `MultiDepartmentQueueing/appsettings.json`, remove the matching department object from `Departments`.
3. In `HospitalQueueCaller/config/users.txt`, remove every user line whose `departmentCode` matches the removed department.
4. Restart the caller app and display app.

Example: to remove `NEW`, delete this department object from both appsettings files:

```json
{ "Name": "New Department", "Code": "NEW", "AccentColor": "#1D4ED8" }
```

Then remove all matching users:

```text
newdepartment|1234|NEW|New Department
```

Synchronization and queue-state notes:

- Removing a department from only one app creates mismatched behavior: callers may still issue numbers that the display does not style correctly, or the display may show a department no caller can use.
- Existing queue state can preserve old departments that were already present in the state file. If you need the removed department to disappear immediately from live state, stop both apps and clear or edit the shared queue state file after backing it up.
- Daily reset clears tickets and current calls, but it does not replace the need to keep both app configuration files synchronized.
- If multiple caller PCs exist, remove the department users from each caller installation's `users.txt`.

## Caller Operation

The caller app can call regular numbers, call priority numbers, skip the current number, and repeat the current call.

- After a successful call, `Repeat Call` is disabled for 5 seconds.
- After a successful repeat, `Repeat Call` is disabled again for 5 seconds.
- During cooldown, the button shows a countdown such as `Repeat Call (5s)`.
- This delay prevents accidental rapid repeat clicks while still allowing staff to repeat the call after a short pause.

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
  "AnnouncementFormat": "Now serving number {Number}. Please proceed to {Counter}, {Department}."
}
```

- Uses installed Windows voices.
- Leave `VoiceName` blank to use the default Windows voice.
- Supported placeholders: `{Number}`, `{Counter}`, `{Department}`, `{Lane}`.
- Configured department codes are automatically spaced for speech, so a ticket like `NEP-R001` is spoken as `N E P-R001`.
- If multiple caller users call numbers at nearly the same time, the display app queues every new call and announces them one by one.
- Speech announcements are processed oldest-to-newest from the newly detected calls, so a newer call does not cancel a call that is already being spoken.

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
dotnet build .\HospitalQueueAdminSetup\HospitalQueueAdminSetup.csproj -o .\verify-build\admin
```

Expected outputs:

- `verify-build/caller/HospitalQueueCaller.exe`
- `verify-build/display/MultiDepartmentQueueing.exe`
- `verify-build/admin/HospitalQueueAdminSetup.exe`

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
