using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Driver
{
    public interface IPookieDriverFactory
    {
        IPookieWebDriver CreateDriver();
    }
}