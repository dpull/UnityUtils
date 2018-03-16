using System;
using System.Collections.Generic;
using System.Text;

namespace X
{
    public static class EventHelper
    {
        //警告    8    CA1030 
        public static void Raise<T>(EventHandler<T> hander, object sender, T args) where T : EventArgs
        {
            //.net设计规范 P128
            var eventHander = hander;
            if (eventHander != null)
            {
                eventHander(sender, args);
            }
        }
    }
}