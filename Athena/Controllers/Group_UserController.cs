using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Athena.Data;
using Athena.Models;
using Microsoft.AspNetCore.Http;

namespace Athena.Controllers
{
    public class Group_UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Group_UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Group_User
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                var applicationDbContext = _context.Group_User.Include(g => g.Group).Include(g => g.User);
                return View(await applicationDbContext.ToListAsync());
            }
            else { return RedirectToAction("Error", "Groups"); }            
        }

       
           
         public IActionResult Create(int id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                var group = _context.Group.Where(m => m.GroupId == id);
                ViewData["GroupId"] = new SelectList(group, "GroupId", "GroupName");
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName");
                return View();
            }
            else { return RedirectToAction("Error", "Groups"); }
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,UserId")] Group_User group_User)
        {
            var GroupId = group_User.GroupId;
            var check = from gu in _context.Group_User
                        where gu.GroupId == group_User.GroupId && gu.UserId == group_User.UserId
                        select gu;

            if (ModelState.IsValid && check.Count() == 0)
            {
                _context.Add(group_User);
                await _context.SaveChangesAsync();
                return RedirectToAction("User", "Groups", new { id = GroupId });
            }
          
            return RedirectToAction("User", "Groups", new { id = GroupId });
        }

        
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var group_User = await _context.Group_User.FindAsync(id);
                if (group_User == null)
                {
                    return NotFound();
                }
                ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupName", group_User.GroupId);
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", group_User.UserId);
                return View(group_User);
            }
            else { return RedirectToAction("Error", "Groups"); }
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupId,UserId")] Group_User group_User)
        {
            if (id != group_User.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(group_User);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Group_UserExists(group_User.Id))
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
            ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupName", group_User.GroupId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", group_User.UserId);
            return View(group_User);
        }

        // GET: Group_User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var group_User = await _context.Group_User
                    .Include(g => g.Group)
                    .Include(g => g.User)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (group_User == null)
                {
                    return NotFound();
                }

                return View(group_User);
            }
            else { return RedirectToAction("Error", "Groups"); }
        }

        // POST: Group_User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group_User = await _context.Group_User.FindAsync(id);

            int GroupId = group_User.GroupId;

            _context.Group_User.Remove(group_User);
            await _context.SaveChangesAsync();
            return RedirectToAction("User", "Groups", new { id = GroupId });
        }

        private bool Group_UserExists(int id)
        {
            return _context.Group_User.Any(e => e.Id == id);
        }
    }
}
