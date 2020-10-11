using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Athena.Data;
using Athena.Models;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;

namespace Athena.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            { return View(await _context.Group.ToListAsync()); }
            else { return View("Error"); }
   
        }
        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Template(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var @group = _context.Group_Template
                .Include(g => g.Group)
                .Include(g => g.Template)
                .Where(m => m.GroupId == id)
                ;
            var @group2 = _context.Group.Find(id);

            if (@group == null)
            {
                return NotFound();
            }
            GroupSearch gs = new GroupSearch
            {
                group = @group2,
                group_template = @group

            };
            
                        
            return View(gs);
        }

        // GET: Groups/Create


        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {  
                return View();
            }
            else { return View("Error"); }           
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,GroupName")] Group @group)
        {
            var check = from g in _context.Group
                        where g.GroupName == @group.GroupName
                        select g;

            if (ModelState.IsValid && check.Count() == 0)
            {
                _context.Add(@group);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        public async Task<IActionResult> User(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var @group = _context.Group_User
                 .Include(g => g.Group)
                 .Include(g => g.User)
                 .Where(m => m.GroupId == id)
                 ;
            var @group2 = _context.Group.Find(id);

            if (@group == null)
            {
                return NotFound();
            }
            GroupSearch gs = new GroupSearch
            {
                group = @group2,
                group_user = @group
               
            };
            
            return View(gs);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var @group = await _context.Group.FindAsync(id);
                if (@group == null)
                {
                    return NotFound();
                }
                return View(@group);
            }
            else { return View("Error"); }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupId,GroupName")] Group @group)
        {
            if (id != @group.GroupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@group);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(@group.GroupId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var @group = await _context.Group
                    .FirstOrDefaultAsync(m => m.GroupId == id);
                if (@group == null)
                {
                    return NotFound();
                }

                return View(@group);
            }
            else { return View("Error"); }
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
           
            var @group = await _context.Group.FindAsync(id);
            _context.Group.Remove(@group);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Group.Any(e => e.GroupId == id);
        }
    }
}
