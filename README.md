# MailNotify

[🇷🇺 Читать на русском](README.ru.md)

**MailNotify** is a Windows desktop application that sends native notifications for Microsoft Exchange calendar events.

<img width="371" height="138" alt="notify" src="https://github.com/user-attachments/assets/15cd67aa-3a0d-4836-a39b-f44294090552" />
<img width="184" height="114" alt="next" src="https://github.com/user-attachments/assets/7a993bb9-4b72-42aa-8ce8-88422e6b4d99" />
<img width="201" height="251" alt="tray" src="https://github.com/user-attachments/assets/7b821816-cc03-4de6-81ea-4e43dd0d11cd" />

## Features

- 📅 Retrieve calendar events from Microsoft Exchange
- 🔔 Native Windows toast notifications
- 🗓️ Notification summarizing upcoming meetings for today and new meetings added throughout the day
- 🖥️ System tray integration: smart icon with auto-detected taskbar theme and real-time hover status
- 📋 Quick-access tray menu (right-click): browse meetings, open call links, and manage settings
- 🔐 Windows authentication (Single Sign-On, no password prompts)
- ⚙️ Runs quietly in the background
- ❌ Daily meeting summary notifications stay visible until manually dismissed
- ⏱️ Pre-meeting reminder notifications automatically dismiss when the meeting begins
- 👆 Clicking a notification directly opens the meeting link
- 🐞 File logging (see `logs` folder in the application root)

## Configuration

The application is configured in the **appsettings.json** file.

Application autostart can be enabled or disabled in the configuration. Enabled by default.

Example: ```"AutoStart": true```

Notifications for upcoming meetings for today as well as new or updated meetings added during the day can be toggled via configuration. Enabled by default.

Example: ```"NotifyDailyAppointments": true```

The Microsoft Exchange server address must be specified in the configuration file.

Example: ```"MailUrl": "https://mail.company.ru"```

Full URL of the web calendar to open from the tray context menu (optional; defaults to `MailUrl` if omitted).

Example: ```"CalendarUrl": "https://mail.company.ru/owa/#path=/calendar"```

Advance reminder time in minutes before a meeting starts. Default is 15 minutes.

Example: ```"ReminderOffsetMinutes": 15```

Interval in minutes for fetching updated calendar data from the server. Default is 3 minutes.

Example: ```"UpdateOffsetMinutes": 3```

## How to Run

Run **MailNotify.exe**

## How It Works

1. The application architecture is split into two independent loops:
   - **Meeting Fetch Loop**: Queries Microsoft Exchange via EWS every `UpdateOffsetMinutes` (default: 3 min), saves the day's schedule into a fast thread-safe in-memory cache, and dispatches daily summaries.
   - **Local Reminder Dispatcher**: Inspects the in-memory schedule every 30 seconds and immediately shows notifications `ReminderOffsetMinutes` before start time without overloading the corporate server with redundant queries.
2. The application uses current Windows user credentials (SSO, without password prompts).
3. Upon startup, it displays a single notification listing today's upcoming meetings.
4. Throughout the day, it only notifies about new or updated meetings that were not previously listed.
5. Meeting summary notifications have no expiration timeout and remain until closed.
6. Sends pre-meeting reminder notifications (time and location displayed on separate lines).
7. Pre-meeting reminders are set to expire right when the meeting starts, so they automatically disappear at kickoff time.

## System Tray Controls

When launched, the application resides in the Windows system tray (notification area next to the clock).

### 🔍 Icon and Hover Status
* **Theme Adaptation:** Automatically detects the Windows taskbar theme and selects a high-contrast icon (light icon for dark taskbar, dark icon for light taskbar).
* **Smart Tooltip:** Hovering over the tray icon displays the current meeting status updated in real time:
  * In a meeting now: `Now: 09:00 (Subject)` — end time of current meeting;
  * No active meeting, but upcoming meetings exist: `Next: 10:30 (Subject)` — start time and subject of the next meeting;
  * No more meetings today: `No upcoming meetings`.

### 📋 Quick-Access Context Menu (Right-Click)
Right-clicking the tray icon opens a modern Windows-styled context menu:
* **Upcoming Meetings:** Lists up to 5 relevant meetings of the day with times (`Now - 14:15` or `15:00 - 15:30`). If a meeting contains a call link (Microsoft Teams, Zoom, Web interface), clicking the item immediately opens it in the browser.
* **Open Web Calendar (📅 Open Web Calendar):** Opens the corporate web calendar page (`CalendarUrl`).
* **Sync Now (🔄 Sync Now):** Triggers an immediate unscheduled poll of the Exchange server and refreshes the schedule without waiting for the background interval.
* **Start with Windows (🚀 Start with Windows):** One-click toggle for autostart, showing current status with a checkmark.
* **Exit (❌ Exit):** Closes the application.

## Requirements

- Windows 10 / Windows 11
- Access to a Microsoft Exchange server
- Windows user account with permissions to access the Exchange calendar

### 🔧 Room for Extension
