namespace Polaris.Native;

/// <summary>
/// An opaque window handle (<c>HWND</c>). Layers above <c>Polaris.Native</c> pass these around
/// instead of raw Win32 types, which stay internal to this assembly.
/// </summary>
/// <param name="Value">The raw handle value.</param>
public readonly record struct WindowHandle(nint Value);
