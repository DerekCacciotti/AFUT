using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace AFUT.Tests.Driver
{
    public interface IPookieWebDriver : IWebDriver, IJavaScriptExecutor
    {
        bool IsDriving { get; }

        void GotoUrl(string url);

        void Login(string username, string password, string url);

        byte[] CaptureScreen();
    }
}