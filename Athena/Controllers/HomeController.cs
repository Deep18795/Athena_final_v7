using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Athena.Models;
using Microsoft.AspNetCore.Http;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Identity;
using k8s;
using Microsoft.AspNetCore.Hosting;
using k8s.Models;

namespace Athena.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, SignInManager<IdentityUser> signInManager, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _env = env;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            string a;
            var UserEmail = this.User.Identity.Name;
            string UserId = _userManager.GetUserId(HttpContext.User);

            if (_signInManager.IsSignedIn(this.User))
            {
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
            return View();
        }

        public IActionResult Error(Exception e)
        {
            ViewBag.Exception = e.Message;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
