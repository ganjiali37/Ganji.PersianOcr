using Emgu.CV;
using Emgu.CV.CvEnum;
using Ganji.PersianOcr.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using Tesseract;

namespace Ganji.PersianOcr.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _hs;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
    {
        _logger = logger;
        _hs = hostingEnvironment;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> Ocr(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest();
            }

            string inputfile = Path.Combine(_hs.WebRootPath, "input.jpg");
            using (var f = new FileStream(inputfile, FileMode.Create))
            {
                await file.CopyToAsync(f);
            }
            string langs = "fa";
            string mode = "tn";
            string tessPath = Path.Combine(_hs.WebRootPath, "tessdata");
            return Json(ProcessImage(inputfile, langs, mode, tessPath));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    static string ProcessImage(string inputfile, string langs, string mode, string tessPath)
    {
        try
        {
            // Load image
            Mat img = CvInvoke.Imread(inputfile, ImreadModes.Color);

            // Resize image
            System.Drawing.Size newSize = new System.Drawing.Size(1024, (int)(1024.0 / img.Width * img.Height));
            CvInvoke.Resize(img, img, newSize, 0, 0, Inter.Linear);

            // Convert to grayscale
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

            // Apply thresholding
            Mat binary = new Mat();
            CvInvoke.Threshold(gray, binary, 170, 255, ThresholdType.Binary);

            // Save processed image (for debugging purposes)
            string processedPath = ".\\wwwroot\\img.jpg";
            binary.Save(processedPath);

            // OCR configuration
            string customConfig = GetTesseractConfig(langs, mode);

            // Perform OCR
            string text = PerformOCR(processedPath, customConfig, tessPath);

            System.IO.File.Delete(processedPath);
            return text;
        }
        catch (Exception)
        {
            throw;
        }
    }

    static string GetTesseractConfig(string langs, string mode)
    {
        if (langs == "fa")
        {
            if (mode == "t")
                return "-l fas --psm 6 -c tessedit_char_blacklist=\"۰١۲۳۴۵۶۷۸۹«»1234567890#\"";
            else if (mode == "tn")
                return "-l fas+eng";
            else if (mode == "table")
                return "-l fas --psm 6 -c tessedit_char_whitelist=\"آابپتثجچحخدذرزژسشصضطظعغفقکگلمنوهی۰١۲۳۴۵۶۷۸۹\"";
        }
        else if (langs == "en")
        {
            return "-l eng --psm 6";
        }
        else if (langs == "faen")
        {
            return "-l fas+eng --psm 6";
        }
        else
        {
            throw new ArgumentException("Choose valid Options.");
        }

        return string.Empty;
    }

    static string PerformOCR(string fileName, string customConfig, string tessdataPath)
    {
        try
        {
            using (var engine = new TesseractEngine(tessdataPath, "fas", EngineMode.Default))
            {
                if (!System.IO.File.Exists(fileName))
                {
                    throw new Exception("file not found.");
                }
                // Load Pix from file
                using (var img = Pix.LoadFromFile(fileName))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
