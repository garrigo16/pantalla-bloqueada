using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

// Autor: Miquel Garrigo
internal static class Activity
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        internal int X, Y;
        internal uint MouseData, Flags, Time;
        internal UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputData { [FieldOffset(0)] internal MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Input { internal uint Type; internal InputData Data; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);

    internal static int MoveOnePixel(int direction)
    {
        Point point;
        if (!GetCursorPos(out point)) throw new Win32Exception("No se pudo comprobar la posición del ratón.");
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(point.X, point.Y));
        int targetX = point.X + (direction < 0 ? -1 : 1);
        if (targetX < screen.Bounds.Left || targetX >= screen.Bounds.Right)
            targetX = point.X + (direction < 0 ? 1 : -1);
        var desktop = Forms.SystemInformation.VirtualScreen;
        // Coordenadas absolutas en el escritorio virtual: un píxel real, sin aceleración.
        int x = (int)(((long)(targetX - desktop.Left) * 65536 + 32768) / desktop.Width);
        int y = (int)(((long)(point.Y - desktop.Top) * 65536 + 32768) / desktop.Height);
        var inputs = new Input[1];
        inputs[0].Data.Mouse = new MouseInput { X = Math.Max(0, Math.Min(65535, x)),
            Y = Math.Max(0, Math.Min(65535, y)), Flags = 0xC001 };
        if (SendInput(1, inputs, Marshal.SizeOf(typeof(Input))) != 1)
            throw new Win32Exception("Windows no permitió generar actividad de ratón.");
        return targetX < point.X ? -1 : 1;
    }
}
