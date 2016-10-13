using System.Collections.Generic;

namespace VideoJoiner.Models
{
    public class Graph
    {
        public Graph()
        {
            data = new List<double>();
        }
        public string name { get; set; }
        public List<double> data { get; set; }
    }
}