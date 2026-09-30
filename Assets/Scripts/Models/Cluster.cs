using System;

namespace HorizonFaser.Models
{
    [Serializable]
    public class Cluster
    {
        public long Id = 0;
        public string Name = "";
        public string Description = "";
        public long? ProjectId;
        public long? ProjectManagerId;
        public System.Collections.Generic.List<Pop> Pops = new System.Collections.Generic.List<Pop>();
    }
}
