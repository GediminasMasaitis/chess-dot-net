using System;

namespace ChessDotNet.Search2
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MinAttribute : Attribute
    {
        public int Min { get; }

        public MinAttribute(int min)
        {
            Min = min;
        }
    }
}