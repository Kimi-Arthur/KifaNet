using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Memrise {
    public class MemriseClient {
        public static string WebDriverUrl { get; set; }
        public static string Cookies { get; set; }

        public void LoadCourse() {
            var options = new ChromeOptions();
            //options.AddArgument("--headless");
            using var driver =
                new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(), TimeSpan.FromMinutes(1));

            driver.Url = "https://app.memrise.com/";
            
            foreach (var cookie in Cookies.Split("; ")) {
                var cookiePair = cookie.Split("=", 2);
                driver.Manage().Cookies.AddCookie(new Cookie(cookiePair[0], cookiePair[1], "app.memrise.com", "/",
                    DateTime.Now + TimeSpan.FromDays(365)));
            }

            driver.Url = "https://app.memrise.com/course/5942698/test-course/edit/database/6977236/";
            Thread.Sleep(TimeSpan.FromSeconds(1000));
        }
    }
}
