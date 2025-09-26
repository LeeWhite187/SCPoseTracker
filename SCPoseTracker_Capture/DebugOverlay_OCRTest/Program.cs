using Emgu.CV.Structure;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Ocl;
using Tesseract;

namespace DebugOverlay_OCRTest
{
    public class Program
    {
        static public string Stored_DebugOverlayFrames_Path = "D:\\Projects\\SCPoseTracker gh\\DebugOverlayFrames\\Raw";
        static public string Stored_DebugOverlayOCRFrames_Path = "D:\\Projects\\SCPoseTracker gh\\DebugOverlayFrames\\OCR";

        static public bool ShowPreProcessedImages = true;
        static public bool SavePreProcessedImages = true;
        static public bool OCRasBitmap = true;

        static public int debugwidth = 1014;
        static public int debugDistanceabove = 0;
        static public int debugheight = 128;

        static public int camposewidth = 640;
        static public int camposeDistanceabove = 7;
        static public int camposeheight = 21;

        static int Main(string[] args)
        {
            Console.WriteLine("Starting...");

            DebugOverlayOCR.Initialize();

            var res = IterateImages();

            Console.WriteLine("Done.");

            return 0;
        }

        static private int IterateImages()
        {
            // Create the output folder if not exist...
            if(!System.IO.Directory.Exists(Stored_DebugOverlayOCRFrames_Path))
                Directory.CreateDirectory(Stored_DebugOverlayOCRFrames_Path);

            // Find the source folder...
            if(!System.IO.Directory.Exists(Stored_DebugOverlayFrames_Path))
            {
                // Doesn't exist.
                return -1;
            }

            // Get image filepaths...
            string[] files;
            try
            {
                files = System.IO.Directory.GetFiles(Stored_DebugOverlayFrames_Path);
            }
            catch (Exception e)
            {
                return -1;
            }

            var starttime = DateTime.Now;
            var imagecount = 0;


            //for(int x  = 0; x < 100; x++)
            //{
            //    var file = files[0];
            //    // Get the file's creation time...
            //    var fct = System.IO.File.GetCreationTime(file);

            //    // Read the file...
            //    using (Bitmap bmp = new Bitmap(file))
            //    {
            //        // Process the current frame...
            //        var res = ProcessFrame(bmp, fct);
            //        if (res != 1)
            //            return -2;
            //    }
            //}

            // Iterate each one...
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;
                if (!System.IO.File.Exists(file))
                    continue;

                // Get the file's creation time...
                var fct = System.IO.File.GetCreationTime(file);

                // Read the file...
                using (Bitmap bmp = new Bitmap(file))
                {
                    // Process the current frame...
                    var res = ProcessFrame(bmp, fct);
                    if (res != 1)
                        return -2;
                }

                imagecount++;
            }

            var endtime = DateTime.Now;
            var duration = (endtime - starttime);
            double timeperframe = (double)(((double)duration.TotalMilliseconds) / imagecount);

            Console.WriteLine($"Image PreProcessing Count: {imagecount.ToString()} images.");
            Console.WriteLine($"Image PreProcessing Duration: {duration.TotalSeconds.ToString()} seconds.");
            Console.WriteLine($"Image PreProcessing Rate: {timeperframe.ToString()} milliseconds per image");

            return 1;
        }


        static private int ProcessFrame(Bitmap bmp, DateTime timestamp)
        {
            //// Process the Cam Pose line...
            //{
            //    // Cleanup the current frame for OCR...
            //    using var preprocCamPose = PreprocessDebugOverlayCamPose_asImageGray(bmp, timestamp);

            //    if(ShowPreProcessedImages)
            //    {
            //        CvInvoke.Imshow("Debug Overlay", preprocCamPose.Mat);
            //        CvInvoke.WaitKey(1);  // Allow frame to render
            //        //CvInvoke.Imshow("Debug Overlay", preprocessed.Mat);
            //        //CvInvoke.WaitKey(1);  // Allow frame to render
            //    }

            //    // Rescale the CamPose box...
            //    using var resizedCamPose = preprocCamPose.Resize(0.5, Inter.Linear);

            //    using var preprocbitmapCamPose = resizedCamPose.ToBitmap();
            //    var ocrCamPose = DebugOverlayOCR.RunOCR_asBitmap(preprocbitmapCamPose, PageSegMode.SingleLine);

            //    // Log the Cam Pose...
            //    Console.WriteLine(ocrCamPose);
            //}

            // Process the Location lines...
            {
                // Cleanup the current frame for OCR...
                using var preprocessed = PreprocessDebugOverlay_asImageGray(bmp, timestamp);

                if (SavePreProcessedImages)
                {
                    // Save the OCR processed file...
                    var pfn = System.IO.Path.Combine(Stored_DebugOverlayOCRFrames_Path, $"overlay-OCRReady-{timestamp:HHmmssfff}.png");
                    preprocessed.Save(pfn);
                }

                //// Rescale the box...
                //using var resized = preprocessed.Resize(0.75, Inter.Linear);
                var resized = preprocessed;

                if (ShowPreProcessedImages)
                {
                    CvInvoke.Imshow("Debug Overlay", resized.Mat);
                    CvInvoke.WaitKey(1);  // Allow frame to render
                }

                string ocrText = "";

                // Run OCR on the position block...
                if(OCRasBitmap)
                {
                    using var preprocbitmap = resized.ToBitmap();
                    ocrText = DebugOverlayOCR.RunOCR_asBitmap(preprocbitmap);
                    //ocrText = DebugOverlayOCR.RunOCR_asBitmap(preprocbitmap, PageSegMode.SingleBlock);
                }
                else
                {
                    ocrText = DebugOverlayOCR.RunOCR_asImageGray(resized);
                    //ocrText = DebugOverlayOCR.RunOCR_asImageGray(resized, PageSegMode.SingleBlock);
                }

                // Log or store the OCR result
                Console.WriteLine(ocrText);
            }

            return 1;
        }

        /// <summary>
        /// This will preprocess the Debug Overlay Region of a given frame for OCR.
        /// It crops the debug overlay, converts it to a grayscale, and blurs it slightly.
        /// NOTE: Don't convert the output of this method back to a Bitmap type,
        ///     unless you're:
        ///         Saving with GDI+ logic,
        ///         Passing the image to an API that needs System.Drawing.Bitmap,
        ///         Displaying it in WinForms.
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        static Image<Gray, byte> PreprocessDebugOverlay_asImageGray(Bitmap bmp, DateTime timestamp)
        {
            // Convert the frame to an Image<Bgr, byte>...
            using var image = bmp.ToImage<Bgr, byte>();

            // Crop the debug overlay region...
            Rectangle overlayRegion = new Rectangle((bmp.Width - debugwidth), debugDistanceabove, debugwidth, debugheight);
            using var overlayImage = image.Copy(overlayRegion);

            // Convert the debugoverlay region to grayscale...
            using var gray = overlayImage.Convert<Gray, byte>();

            //// Apply a little blurring...
            //using var blurred = gray.SmoothGaussian(3);

            // Posterize it a little...
            var thresholded = gray.ThresholdBinaryInv(new Gray(120), new Gray(255));
            //var thresholded = blurred.ThresholdBinary(new Gray(120), new Gray(255));

            return thresholded;
        }

        /// <summary>
        /// This will preprocess the CamPose line of teh Debug Overlay Region of a given frame for OCR.
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        static Image<Gray, byte> PreprocessDebugOverlayCamPose_asImageGray(Bitmap bmp, DateTime timestamp)
        {
            // Convert the frame to an Image<Bgr, byte>...
            using var image = bmp.ToImage<Bgr, byte>();

            // Crop the debug overlay region...
            Rectangle overlayRegion = new Rectangle((bmp.Width - debugwidth), camposeDistanceabove, debugwidth, camposeheight);
            using var overlayImage = image.Copy(overlayRegion);

            // Convert the debugoverlay region to grayscale...
            using var gray = overlayImage.Convert<Gray, byte>();
            // Apply a little blurring...
            using var blurred = gray.SmoothGaussian(3);

            // Posterize it a little...
            var thresholded = blurred.ThresholdBinary(new Gray(120), new Gray(255));

            return thresholded;
        }
    }
}