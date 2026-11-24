using System;

internal static class PinballTests
{
    private static int count;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; Console.WriteLine("OK " + message); }
    private static int Main()
    {
        try
        {
            var game = new PinballPhysics();
            Check(game.Ready && game.Lives == 3 && game.Score == 0, "El pinball empieza con tres ovillos y espera el lanzamiento");
            game.Step(1); Check(game.Ready && game.VY == 0, "Sin lanzar la bola no cae sola");
            game.Launch(); Check(!game.Ready && game.VY < 0, "Lanzar impulsa el ovillo hacia arriba");
            double velocity = game.VY; game.Step(1.0 / 60); Check(game.VY > velocity, "La gravedad frena el ascenso");
            game.Paused = true; double x = game.X, y = game.Y; game.Step(1);
            Check(game.X == x && game.Y == y, "Pausa mantiene la bola en su sitio"); game.Paused = false;
            game.X = 18; game.Y = 50; game.VX = -100; game.VY = 0; game.Step(0.004);
            Check(game.VX > 0 && game.X >= 18, "La bola rebota en la pared izquierda");
            game.X = 302; game.VX = 100; game.Step(0.004); Check(game.VX < 0 && game.X <= 302, "La bola rebota en la pared derecha");
            game.X = 160; game.Y = 18; game.VY = -100; game.Step(0.004); Check(game.VY > 0 && game.Y >= 18, "La bola rebota en la parte superior");
            game.X = 102; game.Y = 75; game.VX = 0; game.VY = 100; game.Step(0.04);
            Check(game.Score == 100 && game.VY < 0 && game.LastBumper == 0, "Los topes dan puntos y expulsan la bola");
            game.X = 118; game.Y = 355; game.VX = 0; game.VY = 100; game.Left = true; game.Step(0.004);
            Check(game.VY < -300 && game.VX > 0, "La palanca izquierda impulsa hacia arriba y al centro");
            game.X = 202; game.Y = 355; game.VX = 0; game.VY = 100; game.Left = false; game.Right = true; game.Step(0.004);
            Check(game.VY < -300 && game.VX < 0, "La palanca derecha impulsa hacia arriba y al centro");
            game.ReleaseControls(); Check(!game.Left && !game.Right, "Perder el foco suelta ambas palancas");
            var drain = new PinballPhysics(); drain.Launch(); drain.X = 160; drain.Y = 370; drain.VX = 0; drain.VY = 100;
            for (int i = 0; i < 40; i++) drain.Step(1.0 / 60);
            Check(drain.Ready && drain.Lives == 2, "El hueco entre palancas permite perder la bola por el centro");
            for (int remaining = 2; remaining >= 0; remaining--)
            {
                game.X = 160; game.Y = PinballPhysics.Height + PinballPhysics.Radius + 1; game.VY = 100; game.Step(0.001);
                Check(game.Lives == remaining && (remaining == 0 ? game.GameOver : game.Ready), "Perder un ovillo deja " + remaining + " disponibles");
                if (remaining > 0) game.Launch();
            }
            Check(game.Score == 100, "La puntuación se conserva entre ovillos");
            game.Launch(); Check(game.Lives == 3 && game.Score == 0 && !game.Ready, "Espacio reinicia una partida terminada");
            x = game.X; y = game.Y; game.Step(double.NaN); game.Step(double.PositiveInfinity); game.Step(-1);
            Check(game.X == x && game.Y == y, "Ignora intervalos inválidos sin corromper la física");
            game.Step(100); Check(!double.IsNaN(game.X) && !double.IsInfinity(game.Y), "Un bloqueo de renderizado no produce un salto enorme");
            var slow = new PinballPhysics(); var fast = new PinballPhysics(); slow.Launch(); fast.Launch();
            for (int i = 0; i < 30; i++) slow.Step(1.0 / 30);
            for (int i = 0; i < 60; i++) fast.Step(1.0 / 60);
            Check(Math.Abs(slow.X - fast.X) < 0.1 && Math.Abs(slow.Y - fast.Y) < 0.1, "Los pasos pequeños mantienen el comportamiento entre 30 y 60 FPS");
            Console.WriteLine("Superadas " + count + " comprobaciones de pinball."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
