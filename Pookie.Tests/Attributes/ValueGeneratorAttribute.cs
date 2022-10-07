using OpenQA.Selenium.DevTools.V104.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ValueGeneratorAttribute : Attribute
    {
        public object[] Values { get; }
        public bool LockValues { get; set; } = true;

        public ValueGeneratorAttribute(params object[] values)
        {
            Values = values;
        }

        public ValueGeneratorAttribute(Type constants, bool allowEmptyString = false)
        {
            var values = constants.GetFields()
                .Select(x => x.GetValue(null));
            if (allowEmptyString)
            {
                values = values.Prepend("");
            }

            Values = values.ToArray();
        }
    }
}