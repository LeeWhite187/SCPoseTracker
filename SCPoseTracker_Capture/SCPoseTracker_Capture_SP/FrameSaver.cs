using System;
using System.Collections.Generic;
using System.Text;

namespace SCPoseTracker_Capture
{
    public class FrameSaver
    {
        private readonly string _outputDir;

        public FrameSaver(string outputDir)
        {
            _outputDir = outputDir;
            Directory.CreateDirectory(_outputDir);
        }

        public void SaveFrame(Bitmap bitmap, DateTime timestampUtc, int frameIndex)
        {
            try
            {
                var filename = Path.Combine(_outputDir, $"frame_{timestampUtc:yyyyMMdd_HHmmss_fff}_{frameIndex:D6}.png");
                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FrameSaver] Error saving frame: {ex.Message}");
            }
        }
    }


}
