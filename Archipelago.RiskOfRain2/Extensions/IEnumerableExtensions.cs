using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Archipelago.RiskOfRain2.Extensions
{
    public static class IEnumerableExtensions
    {
        private static Random rand = new Random();
        public static T Choice<T>(this IEnumerable<T> self)
        {
            var upper = self.Count();
            return self.Skip(rand.Next(upper)).Take(1).SingleOrDefault();
        }
    }
}
