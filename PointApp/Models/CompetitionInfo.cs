using System;
using System.Collections.Generic;
using System.Text;

namespace PointApp.Models
{
    public class CompetitionInfo
    {
        public enum EventType
        { NONE, DH, SG, GS, SL }


        public enum SexType
        {
            Men = 0,
            Women = 1,
        }

        public string Name { get; set; } = string.Empty;
        public SexType Sex { get; set; } = SexType.Men;
        public EventType Type { get; set; } = EventType.SG;
    }
}
