using System;

// Autor: Miquel Garrigo
internal sealed class PinballPhysics
{
    internal const double Width = 320, Height = 450, Radius = 6;
    internal double X, Y, VX, VY;
    internal bool Left, Right, Paused;
    internal int Score { get; private set; }
    internal int Lives { get; private set; }
    internal bool Ready { get; private set; }
    internal bool GameOver { get { return Lives == 0; } }
    internal int LastBumper = -1;
    internal double BumperFlash;
    private double leftKick, rightKick;
    internal PinballPhysics() { Reset(); }
    internal void Reset()
    {
        Score = 0; Lives = 3; Left = Right = Paused = false;
        leftKick = rightKick = BumperFlash = 0; LastBumper = -1; RestBall();
    }
    private void RestBall() { X = 286; Y = 405; VX = VY = 0; Ready = true; }
    internal void Launch()
    {
        if (Paused) return;
        if (GameOver) Reset();
        if (!Ready) return;
        Ready = false; VX = -48; VY = -540;
    }
    internal void ReleaseControls() { Left = Right = false; }
    internal void Step(double elapsed)
    {
        if (double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed <= 0 || Paused || Ready || GameOver) return;
        double remaining = Math.Min(0.05, elapsed);
        while (remaining > 0)
        {
            double dt = Math.Min(1.0 / 240, remaining); remaining -= dt;
            leftKick = Math.Max(0, leftKick - dt); rightKick = Math.Max(0, rightKick - dt);
            BumperFlash = Math.Max(0, BumperFlash - dt);
            VY += 330 * dt; X += VX * dt; Y += VY * dt;
            if (X < 12 + Radius) { X = 12 + Radius; VX = Math.Abs(VX) * 0.86; }
            if (X > Width - 12 - Radius) { X = Width - 12 - Radius; VX = -Math.Abs(VX) * 0.86; }
            if (Y < 12 + Radius) { Y = 12 + Radius; VY = Math.Abs(VY) * 0.86; }
            Circle(102, 103, 19, 100, 0); Circle(218, 103, 19, 100, 1); Circle(160, 190, 22, 150, 2);
            Circle(65, 308, 17, 25, 3); Circle(255, 308, 17, 25, 4);
            Segment(12, 275, 79, 357, 0.72, false, false);
            Segment(308, 275, 241, 357, 0.72, false, false);
            Segment(82, 376, Left ? 142 : 144, Left ? 345 : 402, 0.82, Left, true);
            Segment(238, 376, Right ? 178 : 176, Right ? 345 : 402, 0.82, Right, false);
            double speed = Math.Sqrt(VX * VX + VY * VY);
            if (speed > 650) { VX *= 650 / speed; VY *= 650 / speed; }
            if (Y > Height + Radius)
            {
                Lives--; ReleaseControls();
                if (Lives > 0) RestBall(); else { VX = VY = 0; Ready = false; }
                break;
            }
        }
    }
    private void Circle(double x, double y, double radius, int points, int index)
    {
        double dx = X - x, dy = Y - y, distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance >= radius + Radius) return;
        if (distance < 0.0001) { dx = 0; dy = -1; distance = 1; }
        double nx = dx / distance, ny = dy / distance;
        X = x + nx * (radius + Radius + 0.1); Y = y + ny * (radius + Radius + 0.1);
        double incoming = VX * nx + VY * ny;
        if (incoming >= 0) return;
        VX -= 1.9 * incoming * nx; VY -= 1.9 * incoming * ny;
        VX += nx * 110; VY += ny * 110;
        Score += points; LastBumper = index; BumperFlash = 0.16;
    }
    private void Segment(double ax, double ay, double bx, double by, double bounce, bool raised, bool left)
    {
        double dx = bx - ax, dy = by - ay;
        double t = Math.Max(0, Math.Min(1, ((X - ax) * dx + (Y - ay) * dy) / (dx * dx + dy * dy)));
        double cx = ax + t * dx, cy = ay + t * dy;
        double nx = X - cx, ny = Y - cy, distance = Math.Sqrt(nx * nx + ny * ny);
        double thickness = raised || ay == 376 ? 7 : 3;
        if (distance >= Radius + thickness) return;
        if (distance < 0.0001) { nx = 0; ny = -1; distance = 1; }
        nx /= distance; ny /= distance;
        X = cx + nx * (Radius + thickness + 0.1); Y = cy + ny * (Radius + thickness + 0.1);
        double incoming = VX * nx + VY * ny;
        if (incoming < 0) { VX -= (1 + bounce) * incoming * nx; VY -= (1 + bounce) * incoming * ny; }
        if (raised && Y < cy + Radius && (left ? leftKick : rightKick) <= 0)
        {
            VY = -420 - Math.Min(70, Math.Abs(VY) * 0.12); VX = (left ? 100 : -100) + VX * 0.25;
            if (left) leftKick = 0.18; else rightKick = 0.18;
        }
    }
}
