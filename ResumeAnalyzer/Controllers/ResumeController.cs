using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ResumeAnalyzer.Models;
using ResumeAnalyzer.Services;

namespace ResumeAnalyzer.Controllers
{
    public class ResumeController : Controller
    {
        private readonly GeminiApiService _geminiService;
        private readonly FileProcessingService _fileService;

        public ResumeController()
        {
            _geminiService = new GeminiApiService();
            _fileService = new FileProcessingService();
        }

        // GET: Resume/Upload
        public ActionResult Upload()
        {
            return View(new ResumeViewModel());
        }

        // POST: Resume/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Upload(ResumeViewModel model)
        {
            string savedFilePath = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (model.ResumeFile == null || model.ResumeFile.ContentLength == 0)
                {
                    ModelState.AddModelError("ResumeFile", "Please upload a valid file");
                    return View(model);
                }

                // Validate file
                if (!_fileService.IsValidFile(model.ResumeFile.FileName, model.ResumeFile.ContentLength))
                {
                    ModelState.AddModelError("ResumeFile",
                        "Invalid file. Please upload a PDF, DOCX, or TXT file under 10MB");
                    return View(model);
                }

                // Get App_Data folder path
                string appDataPath = Server.MapPath("~/App_Data");

                // Create App_Data folder if it doesn't exist
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // Generate unique filename to avoid conflicts
                string fileExtension = Path.GetExtension(model.ResumeFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                savedFilePath = Path.Combine(appDataPath, uniqueFileName);

                // Save uploaded file to App_Data folder
                model.ResumeFile.SaveAs(savedFilePath);

                // Extract text from the saved file
                string resumeText;
                using (var fileStream = new FileStream(savedFilePath, FileMode.Open, FileAccess.Read))
                {
                    resumeText = _fileService.ExtractTextFromFile(fileStream, model.ResumeFile.FileName);
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    ModelState.AddModelError("ResumeFile",
                        "Could not extract text from the file. Please ensure the file contains readable text.");
                    return View(model);
                }

                // Check if extracted text is meaningful
                if (resumeText.Length < 50)
                {
                    ModelState.AddModelError("ResumeFile",
                        "The extracted text is too short. Please ensure your resume has actual content.");
                    return View(model);
                }

                // Analyze resume using Gemini API
                var analysisResult = await _geminiService.AnalyzeResumeAsync(
                    resumeText,
                    model.JobDescription,
                    model.TargetPosition
                );

                analysisResult.FileName = model.ResumeFile.FileName;

                // Store result in TempData for next page
                TempData["AnalysisResult"] = analysisResult;

                return RedirectToAction("Analysis");
            }
            catch (Exception ex)
            {
                // Log the full error for debugging
                System.Diagnostics.Debug.WriteLine($"Error processing resume: {ex.ToString()}");

                ModelState.AddModelError("", $"Error processing resume: {ex.Message}");
                return View(model);
            }
            finally
            {
                // ALWAYS delete the temporary file to avoid filling up disk space
                if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(savedFilePath);
                    }
                    catch (Exception deleteEx)
                    {
                        // Log but don't throw - file cleanup failure shouldn't break the app
                        System.Diagnostics.Debug.WriteLine($"Failed to delete temp file: {deleteEx.Message}");
                    }
                }
            }
        }

        // GET: Resume/Analysis
        public ActionResult Analysis()
        {
            var result = TempData["AnalysisResult"] as AnalysisResultViewModel;

            if (result == null)
            {
                return RedirectToAction("Upload");
            }

            return View(result);
        }
    }
}