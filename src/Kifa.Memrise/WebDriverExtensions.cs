using OpenQA.Selenium;

namespace Kifa.Memrise; 

public static class WebDriverExtensions {
    public static void GoToUrl(this IWebDriver webDriver, string url) {
        if (webDriver.Url == url) {
            return;
        }

        webDriver.Url = url;
    }
}
