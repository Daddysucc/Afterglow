using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using Afterglow.PlatformInterop.Utils;
using Microsoft.Win32;
using PowerKit.Extensions;

namespace Afterglow.PlatformInterop;

public class RegistrySwitch<T>(
    RegistryHive hive,
    string keyName,
    string entryName,
    T enabledValue,
    // How a stored value is compared to the enabled value (e.g. ignoring the case of a path)
    IEqualityComparer<T>? comparer = null
)
    where T : notnull
{
    private readonly Lock _lock = new();
    private readonly IEqualityComparer<T> _comparer = comparer ?? EqualityComparer<T>.Default;

    // Always the 64-bit view (the only one on 32-bit Windows), because that's where Windows reads
    // its settings from. A 32-bit build would otherwise be redirected to WOW6432Node.
    private RegistryKey OpenBaseKey() => RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

    public bool IsSet
    {
        get
        {
            using (_lock.EnterScope())
            {
                try
                {
                    // This should always be accessible without elevation
                    using var baseKey = OpenBaseKey();
                    using var key = baseKey.OpenSubKey(keyName, false);

                    // A value of another type (e.g. written by another program, or edited by hand)
                    // simply doesn't count as set, rather than failing
                    return key?.GetValue(entryName) is T value
                        && _comparer.Equals(enabledValue, value);
                }
                // Some accounts (e.g. restricted by group policy) can't read every key
                catch (Exception ex)
                    when (ex is SecurityException or UnauthorizedAccessException or IOException)
                {
                    return false;
                }
            }
        }
        set
        {
            using (_lock.EnterScope())
            {
                // Avoid unnecessary changes
                if (IsSet == value)
                    return;

                try
                {
                    using var baseKey = OpenBaseKey();
                    using var key = baseKey.CreateSubKey(keyName, true);

                    if (value)
                        key.SetValue(entryName, enabledValue);
                    else
                        key.DeleteValue(entryName, false);
                }
                catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
                {
                    // Run reg.exe with elevation
                    if (value)
                    {
                        Reg.SetValue(hive.Moniker + '\\' + keyName, entryName, enabledValue);
                    }
                    else
                    {
                        Reg.DeleteValue(hive.Moniker + '\\' + keyName, entryName);
                    }
                }
            }
        }
    }
}
