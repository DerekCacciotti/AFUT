using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Routine
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RoutineAttribute : Attribute
    {
        public string Name { get; set; }
        public string Category { get; set; }

        public RoutineAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RoutineStepAttribute : Attribute
    {
        public int StepNumber { get; }
        public string Name { get; }
        public Type ParentWorkflow { get; }
        public string ParentStep { get; }
        public bool Optional { get; set; }

        public RoutineStepAttribute(int stepNumber, string name)
        {
            StepNumber = stepNumber;
            Name = name;
        }
    }
}