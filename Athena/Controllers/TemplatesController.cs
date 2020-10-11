using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Athena.Data;
using Athena.Models;
using Microsoft.AspNetCore.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using System.IO;
using k8s;
using k8s.Models;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Hosting;

namespace Athena.Controllers
{
    public class TemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public TemplatesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Templates
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            { return View(await _context.Template.ToListAsync()); }
            else { return View("Error"); }
        }

        public IActionResult Error()
        {
            return View();
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var template = await _context.Template
                    .FirstOrDefaultAsync(m => m.TemplateId == id);
                if (template == null)
                {
                    return NotFound();
                }

                return View(template);
            }
            else { return View("Error"); }
        }

        // GET: Templates/Create
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
        public async Task<IActionResult> Create([Bind("TemplateId,Path,TemplateName,Lab")] Template template)
        {
            var check = from t in _context.Template
                        where t.TemplateName == template.TemplateName
                        select t;

            string path = template.Path;

            string msg = CheckLabel(path, "lab");
            if (msg == template.Lab)
            {
                if (ModelState.IsValid && check.Count() == 0)
                {
                    _context.Add(template);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(template);
            }
            else
            {
                return RedirectToAction("Label", new { id = msg });
            }
        }

        public IActionResult Label(string id)
        {
            ViewBag.Error=id;
            return View();
        }

       
      
 
        public string CheckLabel(string path, string label)
        {
            var deserializeYAML = new DeserializerBuilder()
                      .WithNamingConvention(CamelCaseNamingConvention.Instance)
                      .Build();

            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(_env.WebRootPath + "\\Athena.yaml");
            IKubernetes client = new Kubernetes(config);

           


            string name = null;
            bool test;
            string msg = "";

            string pService = "./" + path + "/Service";
            string pIngress = "./" + path + "/Ingress";
            string pNetPol = "./" + path + "/NetworkPolicy";
            string pDeployment = "./" + path + "/Deployment";

            if (Directory.Exists(pDeployment))
            {
                if (Directory.Exists(pService) == false)
                {
                    pService = null;
                }
                if (Directory.Exists(pIngress) == false)
                {
                    pIngress = null;
                }
                if (Directory.Exists(pNetPol) == false)
                {
                    pNetPol = null;
                }
            }

            else
            {
                pDeployment = null;
            }

            if (pDeployment != null)
            {
                test = CheckDeployments(client, deserializeYAML, pDeployment, label, out name);
                if (test)
                {
                    if (pService != null)
                    {
                        test = CheckServices(client, deserializeYAML, pService, label, name);
                        if (test == false) { msg = "At least one Service file was configured incorrectly"; }

                    }
                    if (pIngress != null && test == true)
                    {
                        test = CheckIngress(client, deserializeYAML, pIngress, label, name);
                        if (test == false) { msg = "At least one Ingress file was configured incorrectly"; }
                    }
                    if (pNetPol != null && test == true)
                    {
                        test = CheckNetPol(client, deserializeYAML, pNetPol, label, name);
                        if (test == false) { msg = "At least one NetworkPolicy file was configured incorrectly"; }
                    }
                    if (test)
                    {
                       
                        msg = name;
                    }

                }
                else
                {
                    if (name != null) { msg = "At least one Deployment file was configured incorrectly"; }
                    else { msg = "Label " + label + " missing from Deployment"; }

                }
            }
            else
            {
                msg = "Template must contain Deployments";
            }

            return msg;
        }
        
        public bool CheckDeployments(IKubernetes client, IDeserializer deserializer, string path, string key, out string value)
        {

            value = null;
            string curr;
            bool match = true;

            foreach (string file in Directory.EnumerateFiles(path))
            {
                
                var fileContent = System.IO.File.ReadAllText(file);
                
                var deployment = Yaml.LoadFromString<V1Deployment>(fileContent);

                if (deployment == null)
                {
                    match = false;
                    break;
                }

                // Check if Deployment has label - if no labels return false

                if (deployment.Metadata.Labels != null)
                {
                    deployment.Metadata.Labels.TryGetValue(key, out curr);
                }

                else
                {
                    match = false;
                    break;
                }

                
                // Matches first Deployment with label, assigns label value to labName

                if (value == null)
                {
                    value = curr;
                }

                // If there is no match at this stage, there is a mismatch

                else if (value != curr)
                {

                    match = false;
                    break;
                }

                // Checking selectors - all deployments must select based on at least the lab name and that name must match on every template

                // Checking MatchLabels

                
                if (deployment.Spec.Selector.MatchLabels != null)
                {
                    deployment.Spec.Selector.MatchLabels.TryGetValue(key, out curr);

                    if (value != curr)
                    {
                        match = false;
                        break;
                    }
                }


                
                // Checking MatchExpressions
                else
                {
                                      
                    
                    var expressions = deployment.Spec.Selector.MatchExpressions;
                    
                    if (expressions != null)
                    {
                        foreach (var expression in expressions)
                        {
                            if (expression.Key == key && expression.OperatorProperty == "In" && expression.Values.Contains(value))
                            {
                                match = true;
                                break;

                            }
                            else
                            {
                                match = false;
                                break;
                            }

                        }
                    }
                    

                    
                    
                }

                // Checking Pod Spec - label should be present and correct
                if (deployment.Spec.Template.Metadata.Labels==null)
                {
                    match = false;
                    break;
                }
                    
                deployment.Spec.Template.Metadata.Labels.TryGetValue(key, out curr);
                                                
                if (value != curr)
                {
                    match = false;
                    break;
                }
                

            }
            return match;
        }

        public bool CheckServices(IKubernetes client, IDeserializer deserializer, string path, string key, string value)
        {
            string curr = null;
            bool match = true;

            foreach (string file in Directory.EnumerateFiles(path))
            {
                
                var fileContent = System.IO.File.ReadAllText(file);

                var service = Yaml.LoadFromString<V1Service>(fileContent);
             
                if (service == null)
                {
                    match = false;
                    break;
                }

                // Check if NetworkPolicy has label - if no label return false
                if (service.Metadata.Labels == null)
                {
                    match = false;
                    break;
                }
                service.Metadata.Labels.TryGetValue(key, out curr);
                  
                // If there is no match at this stage, there is a mismatch

                if (value != curr)
                {

                    match = false;
                    break;
                }
                if (service.Spec.Selector != null)
                {
                    service.Spec.Selector.TryGetValue(key, out curr);
                }
                
                else
                {
                    match = false;
                    break;
                }
                

                if (value != curr)
                {
                    match = false;
                    break;
                }

            }

            return match;
        }
        public bool CheckIngress(IKubernetes client, IDeserializer deserializer, string path, string key, string value)
        {

            string curr;
            bool match = true;

            foreach (string file in Directory.EnumerateFiles(path))
            {
                
                
                var fileContent = System.IO.File.ReadAllText(file);

                var ingress = Yaml.LoadFromString<Networkingv1beta1Ingress>(fileContent);
            

                if (ingress == null)
                {
                    match = false;
                    break;
                }

                // Check if NetworkPolicy has label - if no label return false
                if (ingress.Metadata.Labels == null)
                {
                    match = false;
                    break;
                }
                // Check if Ingress has label - if no label return false
                ingress.Metadata.Labels.TryGetValue(key, out curr);
                

                // If there is no match at this stage, there is a mismatch

                if (value != curr)
                {
                    match = false;
                    break;
                }
            }
            return match;
        }

        static bool CheckNetPol(IKubernetes client, IDeserializer deserializer, string path, string key, string value)
        {
            string curr = "";
            bool match = true;

            foreach (string file in Directory.EnumerateFiles(path))
            {

                
                
                var fileContent = System.IO.File.ReadAllText(file);

                var netPol = Yaml.LoadFromString<V1NetworkPolicy>(fileContent);
               

                if (netPol == null)
                {
                    match = false;
                    break;
                }

                if (netPol.Metadata.Labels != null)
                {
                    netPol.Metadata.Labels.TryGetValue(key, out curr);
                    
                }
                else
                {
                    match = false;
                    break;
                }

                // If there is no match at this stage, there is a error of some kind

                if (value != curr)
                {

                    match = false;
                    break;
                }

            }
            return match;
        }

        // GET: Templates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            { 
                if (id == null)
                {
                    return NotFound();
                }

                var template = await _context.Template.FindAsync(id);
                if (template == null)
                {
                    return NotFound();
                }
                return View(template);
            }
            else { return View("Error"); }            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TemplateId,Path,TemplateName,Lab")] Template template)
        {
            if (id != template.TemplateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(template);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TemplateExists(template.TemplateId))
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
            return View(template);
        }

        // GET: Templates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") == "staff")
            {
                if (id == null)
                {
                    return NotFound();
                }

                var template = await _context.Template
                    .FirstOrDefaultAsync(m => m.TemplateId == id);
                if (template == null)
                {
                    return NotFound();
                }

                return View(template);
            }
            else { return View("Error"); }          
        }

        // POST: Templates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
   
            var template = await _context.Template.FindAsync(id);
            _context.Template.Remove(template);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TemplateExists(int id)
        {
            return _context.Template.Any(e => e.TemplateId == id);
        }
    }
}
