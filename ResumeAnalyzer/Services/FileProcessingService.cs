using System;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using Path = System.IO.Path; // Resolve ambiguity with iTextSharp.text.pdf.parser.Path

namespace ResumeAnalyzer.Services
{
    public class FileProcessingService
    {
        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] AllowedExtensions = { ".pdf", ".docx", ".txt" };

        public bool IsValidFile(string fileName, int fileSize)
        {
            if (string.IsNullOrEmpty(fileName) || fileSize <= 0)
                return false;

            if (fileSize > MaxFileSize)
                return false;

            string extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        // Overload for long fileSize (compatibility)
        public bool IsValidFile(string fileName, long fileSize)
        {
            return IsValidFile(fileName, (int)fileSize);
        }

        public string ExtractTextFromFile(Stream fileStream, string fileName)
        {
            try
            {
                string extension = Path.GetExtension(fileName)?.ToLowerInvariant();

                switch (extension)
                {
                    case ".txt":
                        return ExtractTextFromTxt(fileStream);
                    case ".pdf":
                        return ExtractTextFromPdf(fileStream);
                    case ".docx":
                        return ExtractTextFromDocx(fileStream);
                    default:
                        throw new NotSupportedException($"File type {extension} is not supported");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from file: {ex.Message}", ex);
            }
        }

        private string ExtractTextFromTxt(Stream stream)
        {
            try
            {
                // Reset stream position if possible
                if (stream.CanSeek)
                    stream.Position = 0;

                // Read directly from stream - TXT files are simple
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading TXT file: {ex.Message}", ex);
            }
        }

        private string ExtractTextFromPdf(Stream stream)
        {
            try
            {
                // Reset stream position if possible
                if (stream.CanSeek)
                    stream.Position = 0;

                // CRITICAL FIX: Copy to memory stream first for Somee hosting
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    using (var pdfReader = new PdfReader(memoryStream))
                    {
                        var text = new StringBuilder();

                        for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                        {
                            try
                            {
                                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                                string pageText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);
                                text.AppendLine(pageText);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue with other pages
                                text.AppendLine($"[Error reading page {page}: {ex.Message}]");
                            }
                        }

                        return text.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading PDF file: {ex.Message}", ex);
            }
        }

        private string ExtractTextFromDocx(Stream stream)
        {
            try
            {
                // Reset stream position if possible
                if (stream.CanSeek)
                    stream.Position = 0;

                // CRITICAL FIX: Copy to memory stream first for Somee hosting
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, false))
                    {
                        var body = wordDoc.MainDocumentPart?.Document?.Body;
                        if (body == null)
                            return string.Empty;

                        var text = new StringBuilder();

                        foreach (var paragraph in body.Descendants<Paragraph>())
                        {
                            text.AppendLine(paragraph.InnerText);
                        }

                        return text.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading DOCX file: {ex.Message}", ex);
            }
        }

        // Helper methods
        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName)?.ToLowerInvariant();
        }

        public string GetFileSizeFormatted(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}