using System;
using System.Web.Mvc;
using ResumeAnalyzer.Models;

namespace ResumeAnalyzer.Controllers
{
    public class ContactController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();

        // GET: Contact/Index
        public ActionResult Index()
        {
            return View(new ContactMessage());
        }

        // POST: Contact/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedAt = DateTime.Now;
                    db.ContactMessages.Add(model);
                    db.SaveChanges();

                    TempData["Success"] = "Thank you! Your message has been sent successfully. We'll get back to you within 24-48 hours.";
                    return RedirectToAction("Index");
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