using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

// Autor: Miquel Garrigo
internal sealed partial class LockController
{
    private EasterEggEngine fun;
    private Point? lastPointer;
    private LockWindow FunWindow()
    { foreach (var window in windows) if (window.HasImage) return window; return null; }
    private void InitializeFun()
    { fun = new EasterEggEngine(delegate { return settings.Humor; }, delegate { return DateTime.UtcNow; }); }
    private static bool CommandModifier()
    {
        return (Keyboard.Modifiers & (ModifierKeys.Windows | ModifierKeys.Control | ModifierKeys.Alt)) != 0 ||
            (Native.GetAsyncKeyState(0x5b) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5c) & 0x8000) != 0;
    }
    private static bool Modifier(Key key)
    { return key == Key.LWin || key == Key.RWin || key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift || key == Key.System; }
    private static FunKey Translate(Key key)
    {
        switch (key)
        {
            case Key.Escape: return FunKey.Escape; case Key.Enter: return FunKey.Enter;
            case Key.Space: return FunKey.Space; case Key.F1: return FunKey.F1;
            case Key.Up: return FunKey.Up; case Key.Down: return FunKey.Down;
            case Key.Left: return FunKey.Left; case Key.Right: return FunKey.Right;
            case Key.A: return FunKey.A; case Key.B: return FunKey.B;
            default: return key >= Key.A && key <= Key.Z ? FunKey.Letter : FunKey.Other;
        }
    }
    private void FunKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat || Modifier(e.Key) || CommandModifier()) return;
        var target = FunWindow(); if (target == null) return;
        if (settings.WakeOnInput) target.Peek();
        target.React(fun.Key(Translate(e.Key), false, false), null);
        if (e.Key == Key.Escape || e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.F1) e.Handled = true;
    }
    private void FunTextInput(object sender, TextCompositionEventArgs e)
    {
        if (CommandModifier()) return;
        var target = FunWindow(); if (target != null) target.React(fun.Text(e.Text, false), null);
    }
    private void FunMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (CommandModifier()) return;
        var target = FunWindow(); if (target == null) return;
        var source = (LockWindow)sender;
        target.React(fun.Click(e.ClickCount, source.HitTarget(e.GetPosition(source))), null);
    }
    private void FunMouseMove(object sender, MouseEventArgs e)
    {
        var target = FunWindow(); if (target == null || CommandModifier()) return;
        try
        {
            var source = (LockWindow)sender;
            Point point = source.PointToScreen(e.GetPosition(source));
            if (lastPointer.HasValue)
                target.React(fun.Move((point - lastPointer.Value).Length, target.IsDark), target.PointFromScreen(point));
            lastPointer = point;
        }
        catch (InvalidOperationException) { }
    }
}

internal sealed partial class LockWindow
{
    private FunOverlay funOverlay;
    private FrameworkElement timePanelElement, networkIconElement;
    private AppSettings preferences;
    private bool darkened;
    private readonly DispatcherTimer peekTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
    internal event EventHandler<KeyEventArgs> KeyPressed;
    internal event EventHandler<TextCompositionEventArgs> TextEntered;
    internal event EventHandler<MouseButtonEventArgs> MousePressed;
    internal event EventHandler<MouseEventArgs> PointerMoved;

    private void InitializeFunWindow()
    {
        funOverlay = new FunOverlay(Surface, timePanelElement);
        PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (KeyPressed != null) KeyPressed(this, e); };
        PreviewTextInput += delegate(object sender, TextCompositionEventArgs e) { if (TextEntered != null) TextEntered(this, e); };
        MouseDown += delegate(object sender, MouseButtonEventArgs e) { if (MousePressed != null) MousePressed(this, e); };
        MouseMove += delegate(object sender, MouseEventArgs e) { if (PointerMoved != null) PointerMoved(this, e); };
        peekTimer.Tick += delegate
        {
            peekTimer.Stop();
            if (darkened && imageLayer != null) { imageLayer.Visibility = Visibility.Collapsed; if (timer != null) timer.Stop(); }
        };
    }
    internal void React(FunReaction reaction, Point? pointer) { if (funOverlay != null) funOverlay.Show(reaction, pointer); }
    internal void Peek()
    {
        if (!darkened || imageLayer == null) return;
        imageLayer.Visibility = Visibility.Visible;
        UpdateClock(); if (timer != null) timer.Start();
        peekTimer.Stop(); peekTimer.Start();
    }
    internal FunTarget HitTarget(Point point)
    {
        if (Contains(networkIconElement, point)) return FunTarget.Network;
        return Contains(timePanelElement, point) ? FunTarget.Clock : FunTarget.Background;
    }
    private bool Contains(FrameworkElement element, Point point)
    {
        if (element == null || !element.IsVisible) return false;
        try { return element.TransformToAncestor(this).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)).Contains(point); }
        catch (InvalidOperationException) { return false; }
    }
    private void DisposeFunWindow() { peekTimer.Stop(); if (funOverlay != null) funOverlay.Dispose(); }
}
