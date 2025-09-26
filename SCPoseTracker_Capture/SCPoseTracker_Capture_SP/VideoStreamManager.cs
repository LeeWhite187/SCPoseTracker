using System;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using SCPoseTracker_Capture;

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

        private bool _capturesingleframe;

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

                _capturesingleframe = false;

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
            _capturesingleframe = false;

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

        public void CaptureSingleFrame(string outputDir)
        {
            _frameSaver = new FrameSaver(outputDir);

            this._capturesingleframe = true;
        }

        public void EnableFrameSaving(string outputDir)
        {
            _frameSaver = new FrameSaver(outputDir);

            _capturesingleframe = false;
        }
        public void DisableFrameSaving()
        {
            _frameSaver = null;
            _capturesingleframe = false;
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
                        // Set resolution to 3840x2160 (4K UHD)
                        _capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 3840);
                        _capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 2160);
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
                        if(bmp != null)
                        {
                            // Raise the event
                            FrameReceived?.Invoke(bmp, LastFrameTimestamp.Value);

                            if (_frameSaver != null)
                            {
                                _frameSaver?.SaveFrame(bmp, DateTime.UtcNow, Interlocked.Increment(ref _frameIndex));
                            }

                            // See if we are to only capture a single frame...
                            if(this._capturesingleframe)
                            {
                                // Capturing a single frame.
                                // We can close the frame saver.

                                this.DisableFrameSaving();
                            }
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