using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Config
{
    public interface IAppConfig
    {
        string AppUrl { get; }
        string UserName { get; }
        string Password { get; }
    }
}