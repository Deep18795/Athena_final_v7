using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using k8s;
using k8s.Models;
using System.Security.Claims;
using Athena.Data;
using Microsoft.AspNetCore.Http;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace Athena.Controllers
{
    public class ViewLabController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ViewLabController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public IActionResult Index()
        {
            // Get deployments 
            var userName = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            const string intServiceName = "no-publish";
            string regexReplace; 
            var TemplateId = HttpContext.Session.GetInt32("TemplateId");
            var labName = _context.Template.Find(TemplateId).Lab;
            
            var reservationId = HttpContext.Session.GetInt32("ReservationId");
            var reservation = _context.Reservation.FirstOrDefault(x => x.ReservationId == reservationId);
            if (reservation == null || (reservation != null && reservation.StartOn >= DateTime.Now && reservation.EndOn <= DateTime.Now))
            {
                return RedirectToAction("InvalidReservation", "Reservations");
            }

            ViewBag.EndTime = reservation.EndOn.ToString("yyyy-MM-ddTHH:mm:ss");

            var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(_env.WebRootPath + "\\Athena.yaml");
            var client = new Kubernetes(k8SClientConfig);

            var labDeployments = client.ListNamespacedDeployment(userName, null, null, null, "lab = " + labName);
            var labPods = client.ListNamespacedPod(userName, null, null, null, "lab = " + labName);
            

            var ipAdd = new Dictionary<string, string>();
            
            // Waits for all Pods to be present before trying to get their IP addresses

            while ( labDeployments.Items.Count != labPods.Items.Count)
            {
                labPods = client.ListNamespacedPod(userName, null, null, null, "lab = " + labName);
                Thread.Sleep(5);
            }


            // Waits for all Pods to have IP addresses before trying to get them
            int readyPods;
            do
            {
                readyPods = 0;
                foreach (var pod in labPods.Items)
                {
                    if (pod.Status.PodIP != null)
                    {
                        readyPods++;
                    }
                        
                }


                if (labDeployments.Items.Count == readyPods)
                {
                    break;
                }

                
                Thread.Sleep(5);
                labPods = client.ListNamespacedPod(userName, null, null, null, "lab = " + labName);



            } while (true);
            
        



            foreach (var deployment in labDeployments.Items)
            {
                var deploymentPods = labPods.Items.Where(a => a.Metadata.Name.Contains(deployment.Metadata.Name) == true);

                ipAdd.Add(deploymentPods.FirstOrDefault().Status.PodIP, deployment.Metadata.Name);
            }
            ViewBag.IPADD = ipAdd;

            // Reading paths from Ingress

            var labIngress = client.ListNamespacedIngress1(userName, null, null, null, "lab = " + labName);
            Dictionary<string, string> paths = new Dictionary<string, string>();
            foreach (var item in labIngress.Items)
            {
                
                if (item.Metadata.Annotations.ContainsKey("athena-regex"))
                {
                    var regex = item.Metadata.Annotations["athena-regex"];
                    regexReplace = "";
                    if (item.Metadata.Annotations.ContainsKey("athena-regex-replace"))
                    {
                        regexReplace = item.Metadata.Annotations["athena-regex-replace"];
                    }


                    foreach (var rule in item.Spec.Rules)
                    {
                        foreach (var path in rule.Http.Paths)
                        {
                            if (path.Backend.ServiceName.Contains(intServiceName) != true)
                            {
                                if (path.Path.Contains(regex))
                                {
                                    paths.Add(("http://" + rule.Host + path.Path).Replace(regex, regexReplace), path.Backend.ServiceName);
                                }
                                else
                                {
                                    paths.Add("http://" + rule.Host + path.Path, path.Backend.ServiceName);
                                }
                            }

                        }
                    }
                }
                else
                {
                    foreach (var rule in item.Spec.Rules)
                    {
                        foreach (var path in rule.Http.Paths)
                        {
                            if (path.Backend.ServiceName.Contains(intServiceName) != true)
                            {
                                paths.Add("http://" + rule.Host + path.Path, path.Backend.ServiceName);
                            }
                        }
                    }
                }
            }

            ViewBag.ingress = paths;


            return View();
        }
    }
}