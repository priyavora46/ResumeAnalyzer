using System;
using System.Web.Mvc;
using ResumeAnalyzer.Models;

namespace ResumeAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        // GET: Home/Contact
        public ActionResult Contact()
        {
            return View(new ContactMessage());
        }

        // POST: Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedAt = DateTime.Now;
                    db.ContactMessages.Add(model);
                    db.SaveChanges();

                    TempData["Success"] = "Thank you! Your message has been sent successfully. We'll get back to you within 24-48 hours.";
                    return RedirectToAction("Contact");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Something went wrong. Please try again later.";
                    return View(model);
                }
            }

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}