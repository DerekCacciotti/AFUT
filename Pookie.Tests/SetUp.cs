using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AFUT.Tests.Seeder;
using System.ComponentModel.DataAnnotations;
using AFUT.Tests.Helpers;

namespace AFUT.Tests
{
    public static class SetUp
    {
        private static readonly DataSeeder seeder = new DataSeeder();

        public static T For<T>(T instance)
        {
            if (instance is null)
            {
                return instance;
            }

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (!(prop.GetValue(instance) is null))
                    continue;

                var attributes = Attribute.GetCustomAttributes(prop);

                foreach (var attr in attributes)
                {
                    if (attr is DisplayAttribute)
                    {
                        continue;
                    }
                    else if (attr is ValueGeneratorAttribute vattr)
                    {
                        SetProperty(vattr, prop, instance);
                    }
                }
            }
            return instance;
        }

        private static void SetProperty(ValueGeneratorAttribute attr, PropertyInfo prop, object instance)
        {
            prop.SetValue(instance, Helpers.Convert.ToType(attr.Values.ToString(), prop.PropertyType));
        }
    }
}