namespace Afterglow.PlatformInterop.Internal;

internal static class NativeModule
{
    // Handle of the process executable, used to register window classes. Unlike the handle of
    // this assembly's module, it's also valid when the app is published as a single file
    // (where assemblies are embedded in the executable rather than loaded from disk).
    public static nint CurrentHandle { get; } = NativeMethods.GetModuleHandle(null);
}
