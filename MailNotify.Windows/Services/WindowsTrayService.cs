using MailNotify.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using System.ComponentModel;

namespace MailNotify.Services;

[method: ActivatorUtilitiesConstructor]
public class WindowsTrayService(
    ITrayMenuPresenter menuPresenter,
    ITrayIconManager iconManager,
    IWindowsThemeService themeService,
    AppointmentStore appointmentStore,
    ILogger<WindowsTrayService> logger) : BackgroundService
{
    private NotifyIcon? notifyIcon;
    private ContextMenuStrip? contextMenu;
    private System.Windows.Forms.Timer? tooltipTimer;
    private Thread? uiThread;

    internal WindowsTrayService(
        ISettingsProvider settingsProvider,
        AppointmentStore appointmentStore,
        IServiceScopeFactory scopeFactory,
        ILogger<WindowsTrayService> logger,
        IHostApplicationLifetime? appLifetime = null,
        IAutoStartService? autoStartService = null,
        ITraySettingsProvider? traySettingsProvider = null)
        : this(
            new TrayMenuPresenter(
                appointmentStore,
                settingsProvider,
                scopeFactory,
                NullLogger<TrayMenuPresenter>.Instance,
                autoStartService,
                appLifetime,
                traySettingsProvider: traySettingsProvider),
            new TrayIconManager(new WindowsThemeService()),
            new WindowsThemeService(),
            appointmentStore,
            logger)
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskCompletionSource = new TaskCompletionSource();

        uiThread = new Thread(() =>
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetColorMode(SystemColorMode.System);

                InitializeTray();
                taskCompletionSource.SetResult();

                stoppingToken.Register(Application.Exit);
                Application.Run();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in tray icon message loop");
                taskCompletionSource.TrySetException(ex);
            }
            finally
            {
                CleanupTray();
            }
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = true;
        uiThread.Start();

        return taskCompletionSource.Task;
    }

    private void InitializeTray()
    {
        var colors = themeService.GetColors();

        contextMenu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = SystemFonts.MenuFont ?? new Font("Segoe UI", 9f),
            Padding = new Padding(2, 4, 2, 4),
            Renderer = new ModernTrayMenuRenderer(colors)
        };

        var _ = contextMenu.Handle;

        contextMenu.SizeChanged += (_, _) => ApplyRoundedRegion(contextMenu);
        contextMenu.VisibleChanged += (_, _) =>
        {
            if (contextMenu.Visible)
                ApplyRoundedRegion(contextMenu);
        };
        contextMenu.Opening += OnContextMenuOpening;

        RebuildContextMenu(contextMenu);

        appointmentStore.AppointmentsChanged += OnAppointmentsChanged;

        tooltipTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        tooltipTimer.Tick += (_, _) => UpdateTooltip();
        tooltipTimer.Start();

        notifyIcon = new NotifyIcon
        {
            Icon = iconManager.GetCurrentThemedIcon(),
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = menuPresenter.GetTooltipText()
        };

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnAppointmentsChanged()
    {
        if (contextMenu is not null && contextMenu.IsHandleCreated)
        {
            try
            {
                contextMenu.BeginInvoke(UpdateTooltip);
            }
            catch { }
        }
        else
            UpdateTooltip();
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            iconManager.RefreshIcons();
            if (contextMenu is not null && contextMenu.IsHandleCreated)
            {
                contextMenu.BeginInvoke(() =>
                {
                    UpdateTrayIcon();
                    if (contextMenu.Renderer is ModernTrayMenuRenderer modernRenderer)
                        modernRenderer.Colors = themeService.GetColors();
                    else
                        contextMenu.Renderer = new ModernTrayMenuRenderer(themeService.GetColors());
                    contextMenu.Invalidate();
                });
            }
            else
                UpdateTrayIcon();
        }
    }

    private static void ApplyRoundedRegion(ContextMenuStrip? menu)
    {
        if (menu is null || menu.Width <= 0 || menu.Height <= 0)
            return;

        using var path = ModernTrayMenuRenderer.CreateRoundedRectangle(new Rectangle(0, 0, menu.Width, menu.Height), 8);
        var oldRegion = menu.Region;
        menu.Region = new Region(path);
        oldRegion?.Dispose();
    }

    private void OnContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (contextMenu is null)
            return;

        UpdateTooltip();
        RebuildContextMenu(contextMenu);
        ApplyRoundedRegion(contextMenu);
    }

    private void RebuildContextMenu(ContextMenuStrip menu)
    {
        var model = menuPresenter.GetMenuModel();
        var itemPadding = new Padding(28, 6, 16, 6);

        // Dispose previous items to avoid leaking GDI/Font resources
        for (var i = menu.Items.Count - 1; i >= 0; i--)
        {
            var item = menu.Items[i];
            menu.Items.RemoveAt(i);
            item.Dispose();
        }

        var titleItem = new ToolStripMenuItem(model.Title)
        {
            Enabled = false,
            Font = new Font(menu.Font, FontStyle.Bold),
            Padding = itemPadding
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        if (model.HasUpcomingMeetings)
        {
            foreach (var meeting in model.UpcomingMeetings)
            {
                var meetingItem = new ToolStripMenuItem(meeting.DisplayText)
                {
                    ToolTipText = meeting.Subject,
                    Enabled = meeting.HasUrl,
                    Padding = itemPadding
                };

                if (meeting.HasUrl)
                    meetingItem.Click += (_, _) => menuPresenter.OpenUrl(meeting.WebUrl!);

                menu.Items.Add(meetingItem);
            }
        }
        else
        {
            menu.Items.Add(new ToolStripMenuItem("No upcoming meetings")
            {
                Enabled = false,
                Padding = itemPadding
            });
        }

        menu.Items.Add(new ToolStripSeparator());

        if (model.HasWebCalendarUrl)
        {
            var openCalendarItem = new ToolStripMenuItem("📅 Open Web Calendar")
            {
                Padding = itemPadding
            };
            openCalendarItem.Click += (_, _) => menuPresenter.OpenUrl(model.WebCalendarUrl!);
            menu.Items.Add(openCalendarItem);
        }

        var syncItem = new ToolStripMenuItem("🔄 Sync Now")
        {
            Padding = itemPadding
        };
        syncItem.Click += (_, _) =>
        {
            menuPresenter.TriggerSync();
            UpdateTooltip();
        };
        menu.Items.Add(syncItem);

        if (model.IsAutoStartSupported)
        {
            var autoStartItem = new ToolStripMenuItem("🚀 Start with Windows")
            {
                Checked = model.IsAutoStartEnabled,
                Padding = itemPadding
            };
            autoStartItem.Click += (_, _) =>
            {
                var newState = !autoStartItem.Checked;
                autoStartItem.Checked = newState;
                menuPresenter.ToggleAutoStart(newState);
            };
            menu.Items.Add(autoStartItem);
        }

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("❌ Exit")
        {
            Padding = itemPadding
        };
        exitItem.Click += (_, _) =>
        {
            menuPresenter.ExitApplication();
            Application.Exit();
        };
        menu.Items.Add(exitItem);
    }

    private void UpdateTrayIcon()
    {
        if (notifyIcon is null)
            return;

        var targetIcon = iconManager.GetCurrentThemedIcon();
        if (targetIcon is not null && !ReferenceEquals(notifyIcon.Icon, targetIcon))
            notifyIcon.Icon = targetIcon;
    }

    private void UpdateTooltip()
    {
        if (notifyIcon is null)
            return;

        try
        {
            var text = menuPresenter.GetTooltipText();
            if (notifyIcon.Text != text)
                notifyIcon.Text = text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update tray tooltip");
        }
    }

    internal string GetTooltipText() => menuPresenter.GetTooltipText();

    internal Icon? GetCurrentThemedIcon() => iconManager.GetCurrentThemedIcon();

    private void CleanupTray()
    {
        appointmentStore.AppointmentsChanged -= OnAppointmentsChanged;

        if (tooltipTimer is not null)
        {
            tooltipTimer.Stop();
            tooltipTimer.Dispose();
            tooltipTimer = null;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        if (contextMenu is not null)
        {
            contextMenu.Region?.Dispose();
            contextMenu.Dispose();
            contextMenu = null;
        }

        iconManager.Dispose();
    }
}
