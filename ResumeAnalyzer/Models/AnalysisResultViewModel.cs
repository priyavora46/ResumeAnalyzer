using System.Collections.Generic;

namespace ResumeAnalyzer.Models
{
    public class AnalysisResultViewModel
    {
        public string FileName { get; set; }
        public int OverallScore { get; set; }
        public string Summary { get; set; }

        public List<SkillAnalysis> Skills { get; set; }
        public List<string> Strengths { get; set; }
        public List<string> Weaknesses { get; set; }
        public List<string> Recommendations { get; set; }
        public List<string> MissingKeywords { get; set; }

        public ContactInfo Contact { get; set; }
        public List<string> Education { get; set; }
        public List<string> Experience { get; set; }

        public Dictionary<string, int> CategoryScores { get; set; }
        public string ATSCompatibility { get; set; }
        public int KeywordMatchPercentage { get; set; }

        // NEW PROPERTIES FOR JOB MATCHING
        public int JobMatchScore { get; set; }
        public List<string> MatchingSkills { get; set; }
        public List<string> MissingSkills { get; set; }

        public AnalysisResultViewModel()
        {
            Skills = new List<SkillAnalysis>();
            Strengths = new List<string>();
            Weaknesses = new List<string>();
            Recommendations = new List<string>();
            MissingKeywords = new List<string>();
            Education = new List<string>();
            Experience = new List<string>();
            CategoryScores = new Dictionary<string, int>();
            Contact = new ContactInfo();
            MatchingSkills = new List<string>();
            MissingSkills = new List<string>();
        }
    }

    public class SkillAnalysis
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Proficiency { get; set; }
    }

    public class ContactInfo
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string LinkedIn { get; set; }
        public string Location { get; set; }
    }
}