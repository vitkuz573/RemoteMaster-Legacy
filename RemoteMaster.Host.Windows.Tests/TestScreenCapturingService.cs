// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Tests;

public class TestScreenCapturingService(IDesktopService desktopService) : ScreenCapturingService(desktopService)
{
    protected override void Init()
    {
        // Implementation for testing
    }

    protected override byte[]? GetFrame()
    {
        // Create a simple 1x1 red pixel image for testing
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Red);

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);

        return ms.ToArray();
    }

    public override IEnumerable<Display> GetDisplays()
    {
        return
        [
            new()
            {
                Name = "Test Display",
                IsPrimary = true,
                Resolution = new Size(1920, 1080)
            }
        ];
    }

    public override void SetSelectedScreen(string displayName)
    {
        SelectedScreen = displayName;
    }

    protected override void RefreshCurrentScreenBounds()
    {
        CurrentScreenBounds = new Rectangle(0, 0, 1920, 1080);
    }

    public override Rectangle CurrentScreenBounds { get; protected set; }

    public override Rectangle VirtualScreenBounds => new(0, 0, 3840, 1080);

    public override string SelectedScreen { get; protected set; } = "Test Display";

    protected override bool HasMultipleScreens => true;
}