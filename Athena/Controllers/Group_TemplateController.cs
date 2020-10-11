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
    public class Group_TemplateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Group_TemplateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Group_Template
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                var applicationDbContext = _context.Group_Template.Include(g => g.Group).Include(g => g.Template);
                return View(await applicationDbContext.ToListAsync());
            }
            else { return RedirectToAction("Error", "Groups"); }
            
        }

        // GET: Group_Template/Create
        public IActionResult Create(int id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                var group = _context.Group.Where(m => m.GroupId == id);
                ViewData["GroupId"] = new SelectList(group, "GroupId", "GroupName");
                ViewData["TemplateId"] = new SelectList(_context.Template, "TemplateId", "TemplateName");
                return View();
            }
            else { return RedirectToAction("Error", "Groups"); } 
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,TemplateId")] Group_Template group_Template)
        {
            var GroupId = group_Template.GroupId;
           
            var check = from gt in _context.Group_Template
                  where gt.GroupId == group_Template.GroupId && gt.TemplateId == group_Template.TemplateId
                  select gt;

            if (ModelState.IsValid && check.Count()== 0)
            {
                _context.Add(group_Template);
                await _context.SaveChangesAsync();
                return RedirectToAction("Template", "Groups", new { id = GroupId }); 
            }
            
            return RedirectToAction("Template", "Groups", new { id = GroupId }); 
            
        }

        // GET: Group_Template/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var group_Template = await _context.Group_Template.FindAsync(id);
                if (group_Template == null)
                {
                    return NotFound();
                }
                ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupId", group_Template.GroupId);
                ViewData["TemplateId"] = new SelectList(_context.Template, "TemplateId", "TemplateId", group_Template.TemplateId);
                return View(group_Template);
            }
            else { return RedirectToAction("Error", "Groups"); }
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupId,TemplateId")] Group_Template group_Template)
        {
            if (id != group_Template.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(group_Template);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Group_TemplateExists(group_Template.Id))
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
            ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupId", group_Template.GroupId);
            ViewData["TemplateId"] = new SelectList(_context.Template, "TemplateId", "TemplateId", group_Template.TemplateId);
            return View(group_Template);
        }

        // GET: Group_Template/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var group_Template = await _context.Group_Template
                    .Include(g => g.Group)
                    .Include(g => g.Template)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (group_Template == null)
                {
                    return NotFound();
                }

                return View(group_Template);
            }
            else { return RedirectToAction("Error", "Groups"); }   
        }


        // POST: Group_Template/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group_Template = await _context.Group_Template.FindAsync(id);

            int GroupId = group_Template.GroupId;

            _context.Group_Template.Remove(group_Template);
            await _context.SaveChangesAsync();
            return RedirectToAction("Template", "Groups", new { id = GroupId });
        }

        private bool Group_TemplateExists(int id)
        {
            return _context.Group_Template.Any(e => e.Id == id);
        }
    }
}
