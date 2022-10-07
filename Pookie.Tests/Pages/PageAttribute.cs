using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Pages
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PageAttribute : Attribute
    {
        public string Name { get; set; }
        public string Category { get; set; }
    }
}