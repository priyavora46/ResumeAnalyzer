using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;   // ✅ REQUIRED

namespace ResumeAnalyzer.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; }
    }
}
