using System.ComponentModel.DataAnnotations;
using System.Web;

namespace ResumeAnalyzer.Models
{
    public class ResumeViewModel
    {
        [Required(ErrorMessage = "Please upload a resume file")]
        [Display(Name = "Resume File (PDF, DOCX, TXT)")]
        public HttpPostedFileBase ResumeFile { get; set; }

        [Display(Name = "Job Description (Optional)")]
        [DataType(DataType.MultilineText)]
        public string JobDescription { get; set; }

        [Display(Name = "Target Position (Optional)")]
        public string TargetPosition { get; set; }
    }
}