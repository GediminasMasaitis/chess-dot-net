using System;

namespace ChessDotNet.Search2
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MaxAttribute : Attribute
    {
        public int Max { get; }

        public MaxAttribute(int max)
        {
            Max = max;
        }
    }
}