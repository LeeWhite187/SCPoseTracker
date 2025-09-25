using System;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using SCPoseTracker_CaptureS;

namespace SCPoseTracker_Capture
{
    public class VideoStreamManager : IDisposable
    {
        private readonly object _lock = new();
        private VideoCapture? _capture;
        private Task? _captureTask;
        private CancellationTokenSource? _cts;
        private int _deviceIndex;
        private volatile int _framecount;

        private FrameSaver? _frameSaver;
        private int _frameIndex = 0;

        public bool IsConnected { get; private set; }

        public bool IsRunning { get; private set; }

        public DateTime? LastFrameTimestamp { get; private set; }

        public int FrameCount { get => this._framecount; }

        public event Action<Bitmap, DateTime>? FrameReceived;


        public VideoStreamManager(int deviceIndex)
        {
            _deviceIndex = deviceIndex;
        }


        public bool Start()
        {
            bool succeeded = false;
            try
            {
                if (IsRunning)
                    return true;

                _cts = new CancellationTokenSource();

                _captureTask = Task.Run(() => CaptureLoop(_cts.Token));

                IsRunning = true;

                succeeded = true;
                return true;
            }
            catch (Exception)
            {
                IsRunning = false;
                return false;
            }
            finally
            {
                if(!succeeded)
                {
                    try
                    {
                        _cts?.Cancel();
                    } catch { }
                    try
                    {
                        _cts?.Dispose();
                    } catch { }
                    _cts = null;

                    try
                    {
                        _captureTask?.Dispose();
                    } catch { }
                    _captureTask = null;
                }
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
            } catch { }
            try
            {
                _captureTask?.Wait();
            } catch { }
            try
            {
                _capture?.Dispose();
            } catch { }
            _capture = null;

            IsRunning = false;
        }

        public void EnableFrameSaving(string outputDir)
        {
            _frameSaver = new FrameSaver(outputDir);
        }
        public void DisableFrameSaving()
        {
            _frameSaver = null;
        }

        private void CaptureLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_capture == null)
                    {
                        _capture = new VideoCapture(_deviceIndex);
                        _capture.ImageGrabbed += OnImageGrabbed;
                        _capture.Start();
                        IsConnected = true;
                        Console.WriteLine("[VideoStreamManager] Stream started.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VideoStreamManager] Error: {ex.Message}. Retrying in 3 seconds...");
                    IsConnected = false;
                    Thread.Sleep(3000);
                }

                Thread.Sleep(500); // Prevent tight loop if capture fails
            }
        }

        private void OnImageGrabbed(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                if (_capture == null || !_capture.IsOpened)
                    return;

                try
                {
                    using var mat = new Mat();
                    _capture.Retrieve(mat);

                    if (!mat.IsEmpty)
                    {
                        _framecount++;
                        LastFrameTimestamp = DateTime.UtcNow;
                        // Optional: process mat or convert to bitmap

                        LastFrameTimestamp = DateTime.UtcNow;
                        Interlocked.Increment(ref _framecount);

                        var bmp = mat.ToBitmap();

                        // Raise the event
                        FrameReceived?.Invoke(bmp, LastFrameTimestamp.Value);

                        if (_frameSaver != null)
                        {
                            _frameSaver?.SaveFrame(bmp, DateTime.UtcNow, Interlocked.Increment(ref _frameIndex));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VideoStreamManager] Frame error: {ex.Message}");
                    IsConnected = false;
                }
            }
        }

        public void Dispose()
        {
            Stop();

            try
            {
                _cts?.Cancel();
            } catch { }
            try
            {
                _cts?.Dispose();
            } catch { }
            _cts = null;
        }
    }
}