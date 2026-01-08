using System;
using System.Linq;
using System.Web.Mvc;
using ResumeAnalyzer.Models;

namespace ResumeAnalyzer.Controllers
{
    public class AdminController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Secret key - change this to something only you know!
        private const string SECRET_KEY = "YOUR_SECRET_KEY";

        // Check secret key for all actions
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string key = Request.QueryString["key"];

            if (key != SECRET_KEY)
            {
                filterContext.Result = new HttpNotFoundResult(); // Show 404 to hide admin
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: Admin
        public ActionResult Index()
        {
            return RedirectToAction("ContactMessages");
        }

        // GET: Admin/ContactMessages
        public ActionResult ContactMessages()
        {
            try
            {
                var messages = db.ContactMessages
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();
                return View(messages);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View();
            }
        }

        // GET: Admin/ViewMessage/5
        public ActionResult ViewMessage(int id)
        {
            try
            {
                var message = db.ContactMessages.Find(id);
                if (message == null)
                {
                    TempData["Error"] = "Message not found.";
                    return RedirectToAction("ContactMessages", new { key = SECRET_KEY });
                }
                return View(message);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("ContactMessages", new { key = SECRET_KEY });
            }
        }

        // POST: Admin/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMessage(int id)
        {
            try
            {
                var message = db.ContactMessages.Find(id);
                if (message != null)
                {
                    db.ContactMessages.Remove(message);
                    db.SaveChanges();
                    TempData["Success"] = "Message deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Message not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting message: " + ex.Message;
            }

            string key = Request.QueryString["key"];
            return RedirectToAction("ContactMessages", new { key = key });
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
