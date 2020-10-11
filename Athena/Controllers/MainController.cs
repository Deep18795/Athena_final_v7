using Microsoft.AspNetCore.Mvc;
using k8s;
using k8s.Models;
using Athena.Data;
using System.Security.Claims;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Athena.Controllers
{
    public class MainController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public MainController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public IActionResult Index()
        {
            string a;
            var UserEmail = this.User.Identity.Name;

            var result = GetUserRoles(UserEmail, "deeppatel@athenaprojectweb.com", out a);

            ViewBag.Tutor = a;
            ViewBag.Admin = result;
            if (a == "/staff")
            {
                HttpContext.Session.SetString("Role", "staff");
            }
            else
            {
                HttpContext.Session.SetString("Role", "student");
            }


            var UserId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(_env.WebRootPath + "\\Athena.yaml");

                var client = new Kubernetes(k8SClientConfig);

                var namespaces = client.ListNamespace(null, null, "metadata.name=" + UserId);

                if (namespaces != null && namespaces.Items.Count > 0)
                {
                    return View();
                }


                var ns = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = UserId
                    }
                };

                var result2 = client.CreateNamespace(ns);

                var netPolFile = "default-network-policy.yaml";
                if (System.IO.File.Exists(netPolFile))
                {
                    var fileContent = System.IO.File.ReadAllText(netPolFile);
                    var netPol = Yaml.LoadFromString<V1NetworkPolicy>(fileContent);
                    client.CreateNamespacedNetworkPolicy(netPol, UserId);
                }

                ViewData["Message"] = "";
                return View();

            }
            catch (Exception e)
            {
                return RedirectToAction("Error", e);
            }

        }

        public IActionResult Error(Exception e)
        {
            ViewBag.Exception = e;
            return View();
        }
        public ServiceAccountCredential GetCredential(string[] scopes, string impEmail)
        {

            return new ServiceAccountCredential(new ServiceAccountCredential.Initializer("athenaweb@athenaweb.iam.gserviceaccount.com")
            {
                Scopes = scopes,
                User = impEmail
            }.FromPrivateKey("-----BEGIN PRIVATE KEY-----\nMIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDHXX6IZS6kyYrl\n8TWf2OjiPAlKlbv02JxXD5uQW2fLE2zrs+DU7t13wKHP8havoYdbxk4/HM6e/39L\nCVQkxi31q+WUVGDQj3Gz+vT8RL6WPIayfc7zWcRHaGbcr/f3saKC5gRJNi8fQcKu\nT0c7iRlqTKNNVVoKF1BHd6bMVnq8APtiwdEqwJsKx6UIADVP5IS521R8wfm9eYH0\nvp8oT9hMLoLTmLs7QjTOuoClad2nH+DeCjaFrQjfjS4FnonQ6IcvVFrXT1VUCwIj\nFRWVTJqHMk3fLoovbIpjz3P/ppCRlXEP+t33n+IGVBPaG5kZtppj/DhTqRL/aGfy\nYzAIYS4vAgMBAAECggEAAzDq6pH8CiUztPSEcDePmrdtSqVU6dlhvz7/tXLRGX4J\nG0i4y4+7OcR6kpKEbHFAcmadMANtBzx9tBzFHQWTDMCgnxwupIf3QPM+Yp9TEaKM\nYWUEH/8K1j3Ej7m9VVihikjWPrnATVbHH/Ui5cAPCRWK85zvIQQ4g7xVjiXANywh\nnOYI3GPFmyPm8O7SO178n0GoVahiOc0uVl9rjuqNkkkGkndwMB52NXM1/gGAeS7K\nkFCoXVIiRHd8/PcqrExG6zNOGDPMD9L5dfZnu5Ave/JKR3RMYyBlVb/mrRRy2vRX\nfwWqwsxL3bgJXeeAYIfjyRgXSD4ksWwBJpevRtcEeQKBgQDrkOSIqaMdS88mizTP\nvON/nThdtR8jEXWTY1687uO1TF7ksATt2jkOfwKNlRieID/mYqJu1mMrPPD7iCba\nkDBDCZCpNwB+7xAwewH7PUVnXZhjd0yzxJPT430l2UfGma+ztifetlALM+5YPRBO\ngP/AGo7agvKsXAvF2TsrfGh5CwKBgQDYqLUTMxyEHM2F2VgrM4e7mIqo2n7+KVqI\nbFbe+Dxt9ajZkKlcvV57/dyxHphkNcnVHiCZE/TLrG5zS2awmooW4hw1h3Eu1H+C\nHT9ZUHuYptmOOGWorYXeu5Drj8QVCO6owzXjE6l8CNet9ibMqbtpixrJ2CyIbAnD\nYuWrlmS97QKBgQC7IpTamGCzYkkDJq1ipnzYIS7pCnzc3/7Wgqd9Ug3lNfFgnRCd\nX7HZ+T4u+ZXf8GCzBgJiKMAJVlVejO/Iy6j7aHraYo5rSEFFMkMFsswS7ICl690s\nJmsFdgAydCUX+XliO7/6pjx6Wdvrjz8IDmSd2LtJjaN5F3pmx0bBKgjYtwKBgQC3\ndFsnL780qsLoVHpQ0mhbU7YNdj3T7qZHIB2K3X0lyr63wsN10K+ho/rsSzDUoasO\nd1044WoF0DMSE0WXwrOs3rbuKIqREcQKI8PRV9HgF1/eCikiZBQX3pC+tdRdz1tu\nsST+61Y2vbILDoQaBpq3qt77DL3gokK+HA7HdShGnQKBgQC3ehccVGQCylFdTzM9\nWijwq9Ec7F3HVpzDE415Vf6Gtr8wy/feTyJYxCn2c8NCae+m2CdG9B3L3TCBiMyF\n220mZnO7fXmdYaXrzOH8P9u5trU1VubBTzQFRh32d7+NZCykQ3roTnCgDviBx03m\n3QRKwxZJ8TzYK83Wihi6u+VcqQ==\n-----END PRIVATE KEY-----\n"));


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
        public async Task<string> DeleteAll()
        {
            ApplicationDbContext c = _context;
            var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(_env.WebRootPath + "\\Athena.yaml");
            var client = new Kubernetes(k8SClientConfig);

            var users = from a in c.Users
                        select a.Id;

            var deplist = client.ListDeploymentForAllNamespaces();

            var namelist = new HashSet<string>();
            foreach (var deployment in deplist.Items)

            {
                if (users.Contains(deployment.Metadata.NamespaceProperty) && namelist.Contains(deployment.Metadata.NamespaceProperty) == false)
                {

                    namelist.Add(deployment.Metadata.NamespaceProperty);
                }

            }


            foreach (var name in namelist)
            {
                CleanLab(client, name);

            }

            return "Deleted Labs";

        }



        public void CleanLab(IKubernetes client, string userName)
        {
            var dList = client.ListNamespacedDeployment(userName);
            V1ServiceList sList = null;
            V1NetworkPolicyList nList = null;
            Networkingv1beta1IngressList iList = null;

            if (dList != null && dList.Items.Count > 0)
            {
                sList = client.ListNamespacedService(userName);
                nList = client.ListNamespacedNetworkPolicy(userName);
                iList = client.ListNamespacedIngress1(userName);
                foreach (var item in dList.Items)
                {
                    client.DeleteNamespacedDeployment(item.Metadata.Name, userName);
                }
            }
            if (sList != null && sList.Items.Count > 0)
            {
                foreach (var item in sList.Items)
                {
                    client.DeleteNamespacedService(item.Metadata.Name, userName);
                }
            }
            if (iList != null && iList.Items.Count > 0)
            {
                foreach (var item in iList.Items)
                {
                    client.DeleteNamespacedIngress1(item.Metadata.Name, userName);
                }
            }
            if (nList != null && nList.Items.Count > 0)
            {
                foreach (var item in nList.Items)
                {
                    client.DeleteNamespacedNetworkPolicy(item.Metadata.Name, userName);
                }
            }
        }
    }
}


