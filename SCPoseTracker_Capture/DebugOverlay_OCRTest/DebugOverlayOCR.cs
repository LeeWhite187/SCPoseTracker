using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Tesseract;
using System.Diagnostics;
using Emgu.CV.CvEnum;

namespace DebugOverlay_OCRTest
{
    static public class DebugOverlayOCR
    {
        static public string Tesseract_TrainedLanguageData_Folderpath = "D:\\Projects\\SCPoseTracker gh\\Tesseract_TrainedLanguageData";

        static public bool SkipLegacyMode = false;

        private static TesseractEngine _engine;

        public static void Initialize(string tessdataPath = "", string lang = "eng")
        {
            if (string.IsNullOrWhiteSpace(tessdataPath))
                tessdataPath = Tesseract_TrainedLanguageData_Folderpath;

            if(SkipLegacyMode)
                _engine = new TesseractEngine(tessdataPath, lang, EngineMode.LstmOnly);
            else
                _engine = new TesseractEngine(tessdataPath, lang, EngineMode.Default);

            _engine.DefaultPageSegMode = PageSegMode.SingleBlock;

            _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789:_-. ");
            _engine.SetVariable("load_system_dawg", "0");
            _engine.SetVariable("load_freq_dawg", "0");
            _engine.SetVariable("load_number_dawg", "0");
            _engine.SetVariable("preserve_interword_spaces", "1");
        }

        public static string RunOCR_asImageGray(Image<Gray, byte> overlayImage)
        {
            if (_engine == null) throw new InvalidOperationException("Tesseract engine not initialized.");

            string outtext = "";

            var sw = Stopwatch.StartNew();

            using var bmp = overlayImage.ToBitmap();
            using var mem = new MemoryStream();
            bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
            mem.Position = 0;

            using var pix = Pix.LoadFromMemory(mem.ToArray());
            using var page = _engine.Process(pix);

            outtext = page.GetText();

            sw.Stop();
            Console.WriteLine($"OCR Time: {sw.Elapsed.TotalMilliseconds}ms");

            return outtext;
        }

        public static string RunOCR_asImageGray(Image<Gray, byte> overlayImage, PageSegMode samplemode)
        {
            if (_engine == null) throw new InvalidOperationException("Tesseract engine not initialized.");

            _engine.DefaultPageSegMode = samplemode;

            string outtext = "";

            var sw = Stopwatch.StartNew();

            using var bmp = overlayImage.ToBitmap();
            using var mem = new MemoryStream();
            bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
            mem.Position = 0;

            using var pix = Pix.LoadFromMemory(mem.ToArray());
            using var page = _engine.Process(pix);

            outtext = page.GetText();

            sw.Stop();
            Console.WriteLine($"OCR Time: {sw.Elapsed.TotalMilliseconds}ms");

            return outtext;
        }

        public static string RunOCR_asBitmap(Bitmap overlayImage)
        {
            if (_engine == null) throw new InvalidOperationException("Tesseract engine not initialized.");

            string outtext = "";

            var sw = Stopwatch.StartNew();

            // Convert Bitmap to Pix using Tesseract.Drawing helper (if available)
            using (var pix = PixConverter.ToPix(overlayImage))
            using (var page = _engine.Process(pix))
            {
                outtext = page.GetText().Trim();
            }

            sw.Stop();
            Console.WriteLine($"OCR Time: {sw.Elapsed.TotalMilliseconds}ms");

            return outtext;
        }
        public static string RunOCR_asBitmap(Bitmap overlayImage, PageSegMode samplemode)
        {
            if (_engine == null) throw new InvalidOperationException("Tesseract engine not initialized.");

            _engine.DefaultPageSegMode = samplemode;

            string outtext = "";

            var sw = Stopwatch.StartNew();

            // Convert Bitmap to Pix using Tesseract.Drawing helper (if available)
            using (var pix = PixConverter.ToPix(overlayImage))
            using (var page = _engine.Process(pix))
            {
                outtext = page.GetText().Trim();
            }

            sw.Stop();
            Console.WriteLine($"OCR Time: {sw.Elapsed.TotalMilliseconds}ms");

            return outtext;
        }
    }
}
