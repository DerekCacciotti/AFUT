using System.Collections.Generic;

namespace AFUT.Tests.Config
{
    public interface IAppConfig
    {
        string AppUrl { get; }
        string UserName { get; }
        string Password { get; }
        IReadOnlyList<string> CaseHomeTabs { get; }
    }
}