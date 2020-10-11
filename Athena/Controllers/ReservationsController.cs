using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Athena.Data;
using Athena.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using k8s;
using Microsoft.AspNetCore.Hosting;
using k8s.Models;

namespace Athena.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            string a;
            var UserEmail = this.User.Identity.Name;
            
            var result = GetUserRoles(UserEmail, "deeppatel@athenaprojectweb.com", out a);

            ViewBag.Tutor = a;
            ViewBag.Admin = result;

            List<Reservation> reservations = new List<Reservation>();

            string currentUserId = _userManager.GetUserId(HttpContext.User);

            if (a == "/staff")
            {
                reservations = await _context.Reservation.Include(r => r.CreatedByUser).Include(r => r.Group).Include(r => r.Template).ToListAsync();
                HttpContext.Session.SetString("Role", "staff");
            }
            else
            {
                HttpContext.Session.SetString("Role", "student");

                reservations = await (from r in _context.Reservation.Include(r => r.CreatedByUser).Include(r => r.Group).Include(r => r.Template)
                                      join gu in _context.Group_User on r.GroupId equals gu.GroupId
                                      where gu.UserId == currentUserId
                                      select r).ToListAsync();
            }


            ViewBag.CurrentUserId = currentUserId;

            try
            {
                var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(_env.WebRootPath + "\\Athena.yaml");

                var client = new Kubernetes(k8SClientConfig);

                var namespaces = client.ListNamespace(null, null, "metadata.name=" + currentUserId);

                if (namespaces != null && namespaces.Items.Count > 0)
                {
                    return View(reservations);
                }

                var ns = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = currentUserId
                    }
                };

                var result2 = client.CreateNamespace(ns);

                var netPolFile = "default-network-policy.yaml";
                if (System.IO.File.Exists(netPolFile))
                {
                    var fileContent = System.IO.File.ReadAllText(netPolFile);
                    var netPol = Yaml.LoadFromString<V1NetworkPolicy>(fileContent);
                    client.CreateNamespacedNetworkPolicy(netPol, currentUserId);
                }

                ViewData["Message"] = "";

            }
            catch (Exception e)
            {
                return RedirectToAction("Error", e);
            }

            return View(reservations);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation
                .Include(r => r.CreatedByUser)
                .Include(r => r.Group)
                .Include(r => r.Template)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            //ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["GroupId"] = GetGroups(null);
            //  ViewData["TemplateId"] = new SelectList(_context.Template, "TemplateId", "Lab");
            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,StartOn,EndOn,GroupId,TemplateId")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                reservation.CreatedByUserId = _userManager.GetUserId(HttpContext.User);
                reservation.CreatedOn = DateTime.Now;

                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            //ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "Id", reservation.CreatedByUserId);
            ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupName", reservation.GroupId);
            ViewData["TemplateId"] = new SelectList(_context.Template, "TemplateId", "Lab", reservation.TemplateId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            ViewData["GroupId"] = GetGroups(reservation.GroupId);

            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,CreatedOn,CreatedByUserId,StartOn,EndOn,GroupId,TemplateId")] Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationId))
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
            ViewData["GroupId"] = GetGroups(reservation.GroupId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation
                .Include(r => r.CreatedByUser)
                .Include(r => r.Group)
                .Include(r => r.Template)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservation.FindAsync(id);
            _context.Reservation.Remove(reservation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservation.Any(e => e.ReservationId == id);
        }


        public async Task<IActionResult> InvalidReservation()
        {
            return View();
        }

        public JsonResult GetTemplates(int groupId)
        {
            var @group = _context.Group_Template
                 .Include(g => g.Group)
                 .Include(g => g.Template)
                 .Where(m => m.GroupId == groupId);

            return Json(new SelectList(@group, "Template.TemplateId", "Template.TemplateName", string.Empty));
        }

        public SelectList GetGroups(int? selectedGroup)
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var @group = _context.Group_User
                 .Include(g => g.Group)
                 .Include(g => g.User)
                 .Where(m => m.UserId == userId);

            return new SelectList(@group, "Group.GroupId", "Group.GroupName", selectedGroup);
        }

        // Takes three parameters - the user to lookup, the user to impersonate and an initialised string variable to store the OU path
        public bool? GetUserRoles(string userID, string serviceEmail, out string ou)
        {
            // DirectoryService creation
            string[] directoryScopes = { DirectoryService.Scope.AdminDirectoryUserReadonly };

            var cred = GetCredential(directoryScopes, serviceEmail);

            DirectoryService service = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred,
                ApplicationName = "Athena",
            });
            var request = service.Users.Get(userID);
            request.ViewType = UsersResource.GetRequest.ViewTypeEnum.AdminView;

            var user = request.Execute();
            ou = user.OrgUnitPath;
            return user.IsAdmin;
        }

        public ServiceAccountCredential GetCredential(string[] scopes, string impEmail)
        {

            return new ServiceAccountCredential(new ServiceAccountCredential.Initializer("athenaweb@athenaweb.iam.gserviceaccount.com")
            {
                Scopes = scopes,
                User = impEmail
            }.FromPrivateKey("-----BEGIN PRIVATE KEY-----\nMIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDHXX6IZS6kyYrl\n8TWf2OjiPAlKlbv02JxXD5uQW2fLE2zrs+DU7t13wKHP8havoYdbxk4/HM6e/39L\nCVQkxi31q+WUVGDQj3Gz+vT8RL6WPIayfc7zWcRHaGbcr/f3saKC5gRJNi8fQcKu\nT0c7iRlqTKNNVVoKF1BHd6bMVnq8APtiwdEqwJsKx6UIADVP5IS521R8wfm9eYH0\nvp8oT9hMLoLTmLs7QjTOuoClad2nH+DeCjaFrQjfjS4FnonQ6IcvVFrXT1VUCwIj\nFRWVTJqHMk3fLoovbIpjz3P/ppCRlXEP+t33n+IGVBPaG5kZtppj/DhTqRL/aGfy\nYzAIYS4vAgMBAAECggEAAzDq6pH8CiUztPSEcDePmrdtSqVU6dlhvz7/tXLRGX4J\nG0i4y4+7OcR6kpKEbHFAcmadMANtBzx9tBzFHQWTDMCgnxwupIf3QPM+Yp9TEaKM\nYWUEH/8K1j3Ej7m9VVihikjWPrnATVbHH/Ui5cAPCRWK85zvIQQ4g7xVjiXANywh\nnOYI3GPFmyPm8O7SO178n0GoVahiOc0uVl9rjuqNkkkGkndwMB52NXM1/gGAeS7K\nkFCoXVIiRHd8/PcqrExG6zNOGDPMD9L5dfZnu5Ave/JKR3RMYyBlVb/mrRRy2vRX\nfwWqwsxL3bgJXeeAYIfjyRgXSD4ksWwBJpevRtcEeQKBgQDrkOSIqaMdS88mizTP\nvON/nThdtR8jEXWTY1687uO1TF7ksATt2jkOfwKNlRieID/mYqJu1mMrPPD7iCba\nkDBDCZCpNwB+7xAwewH7PUVnXZhjd0yzxJPT430l2UfGma+ztifetlALM+5YPRBO\ngP/AGo7agvKsXAvF2TsrfGh5CwKBgQDYqLUTMxyEHM2F2VgrM4e7mIqo2n7+KVqI\nbFbe+Dxt9ajZkKlcvV57/dyxHphkNcnVHiCZE/TLrG5zS2awmooW4hw1h3Eu1H+C\nHT9ZUHuYptmOOGWorYXeu5Drj8QVCO6owzXjE6l8CNet9ibMqbtpixrJ2CyIbAnD\nYuWrlmS97QKBgQC7IpTamGCzYkkDJq1ipnzYIS7pCnzc3/7Wgqd9Ug3lNfFgnRCd\nX7HZ+T4u+ZXf8GCzBgJiKMAJVlVejO/Iy6j7aHraYo5rSEFFMkMFsswS7ICl690s\nJmsFdgAydCUX+XliO7/6pjx6Wdvrjz8IDmSd2LtJjaN5F3pmx0bBKgjYtwKBgQC3\ndFsnL780qsLoVHpQ0mhbU7YNdj3T7qZHIB2K3X0lyr63wsN10K+ho/rsSzDUoasO\nd1044WoF0DMSE0WXwrOs3rbuKIqREcQKI8PRV9HgF1/eCikiZBQX3pC+tdRdz1tu\nsST+61Y2vbILDoQaBpq3qt77DL3gokK+HA7HdShGnQKBgQC3ehccVGQCylFdTzM9\nWijwq9Ec7F3HVpzDE415Vf6Gtr8wy/feTyJYxCn2c8NCae+m2CdG9B3L3TCBiMyF\n220mZnO7fXmdYaXrzOH8P9u5trU1VubBTzQFRh32d7+NZCykQ3roTnCgDviBx03m\n3QRKwxZJ8TzYK83Wihi6u+VcqQ==\n-----END PRIVATE KEY-----\n"));


        }
    }
}
