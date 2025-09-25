using System;
using System.Threading;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace SCPoseTracker_Capture
{
    public class Program
    {
        static int Main(string[] args)
        {
            var cts = new CancellationTokenSource();


            // Adjust index if needed
            using var video = new VideoStreamManager(0);
            video.FrameReceived += OnFrameReceived;
            //video.EnableFrameSaving("frames");
            video.DisableFrameSaving();


            Stopwatch streamClock = new();
            int frameCount = 0;
            DateTime? streamStart = null;
            DateTime lastFrameReceived = DateTime.MinValue;


            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                video.Stop();
            };


            Console.Clear();
            Console.CursorVisible = false;
            Console.WriteLine("Starting video stream...");


            // Start video capture...
            if (!video.Start())
            {
                Console.WriteLine("Failed to start video stream.");
                return 1;
            }

            while (video.IsRunning)
            {
                DrawStatus(video);
                Task.Delay(250).GetAwaiter();
            }

            Console.WriteLine("\nVideo stream ended.");

            return 0;
        }

        static void DrawStatus(VideoStreamManager video)
        {
            Console.SetCursorPosition(0, 2);

            Console.WriteLine("=== Star Citizen Pose Capture: Video Feed Monitor ===");
            Console.WriteLine($" Stream Status       : {(video.IsRunning ? "ACTIVE" : "INACTIVE")}");
            Console.WriteLine($"Frames               : {video.FrameCount}                      ");
            Console.WriteLine($"Last Timestamp       : {video.LastFrameTimestamp:HH:mm:ss.fff}           ");
            //Console.WriteLine($" Stream Start (UTC)  : {(streamStart?.ToString("HH:mm:ss") ?? "---")}");
            //Console.WriteLine($" Frame Count         : {frameCount}");
            //Console.WriteLine($" Last Frame (UTC)    : {lastFrameReceived:HH:mm:ss}");
            //Console.WriteLine($" Fabricated Timestamp: {streamClock.Elapsed:hh\\:mm\\:ss\\.fff}");
            Console.WriteLine();
            Console.WriteLine(" Press [Esc] to exit...");
        }

        static void OnFrameReceived(Bitmap bmp, DateTime timestamp)
        {
            // Optionally do something with each frame here
        }
    }
}
