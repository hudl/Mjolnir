using System.Collections.Generic;

namespace Hudl.Mjolnir.SystemTests
{
    internal class ChartSet
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Chart> Charts { get; set; } 
    }
}