using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WinContainers.Build;

public class FixCulture : Microsoft.Build.Utilities.Task
{
    [Required]
    public string ToolsDir { get; set; } = "";

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, "FixCulture: Hooking satellite assembly resolution for en-DK...");

        // Create en-DK satellite assemblies
        string enDkDir = Path.Combine(ToolsDir, "en-DK");
        string enDir = Path.Combine(ToolsDir, "en");
        Directory.CreateDirectory(enDkDir);

        string enSat = Path.Combine(enDir, "Microsoft.UI.Xaml.Markup.Compiler.resources.dll");
        string[] targets = new[]
        {
            Path.Combine(enDkDir, "Microsoft.UI.Xaml.Markup.Compiler.resources.dll"),
            Path.Combine(enDkDir, "XamlCompiler.resources.dll"),
            Path.Combine(enDkDir, "Microsoft.UI.Xaml.Markup.Compiler.IO.resources.dll"),
        };

        if (File.Exists(enSat))
        {
            foreach (var t in targets)
            {
                if (!File.Exists(t))
                {
                    File.Copy(enSat, t, true);
                    Log.LogMessage(MessageImportance.High, $"FixCulture: Created {t}");
                }
            }
        }

        // Set thread cultures
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        return true;
    }
}
