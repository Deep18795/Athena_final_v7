using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Athena.Data;
using Microsoft.AspNetCore.Http;

namespace Athena.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                return View(await _context.Users.ToListAsync());
            }
            else { return RedirectToAction("Error", "Groups"); }
        }
    }
}