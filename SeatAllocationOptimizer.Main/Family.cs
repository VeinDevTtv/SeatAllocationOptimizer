using System;
using System.Collections.Generic;
using System.Linq;

namespace SeatAllocationOptimizer.Main
{
    public class Family
    {
        public List<Passenger> Members { get; private set; } = new List<Passenger>();

        public double TotalRevenue => Members.Sum(p => p.Revenue);
        public double AverageRevenue => Members.Any() ? TotalRevenue / Members.Count : 0;
        public int TotalSeatsNeeded => Members.Sum(p => p.SeatsNeeded);
        public bool HasChildren => Members.Any(p => !p.IsAdult);

        public void AddMember(Passenger passenger)
        {
            if (passenger != null)
            {
                Members.Add(passenger);
            }
        }

        // TODO: Add methods for adding members, etc., as needed.
    }
} 