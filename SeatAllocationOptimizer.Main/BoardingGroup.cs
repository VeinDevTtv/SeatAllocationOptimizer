using System;

namespace SeatAllocationOptimizer.Main
{
    public class BoardingGroup
    {
        public Passenger? IndividualPassenger { get; private set; }
        public Family? FamilyGroup { get; private set; }

        public bool IsFamily => FamilyGroup != null;

        public double GroupRevenue => IsFamily ? FamilyGroup!.TotalRevenue : IndividualPassenger!.Revenue;
        public int SeatsNeeded => IsFamily ? FamilyGroup!.TotalSeatsNeeded : IndividualPassenger!.SeatsNeeded;
        public double RevenuePerSeat => SeatsNeeded > 0 ? GroupRevenue / SeatsNeeded : 0;

        // Constructor for individual passenger
        public BoardingGroup(Passenger passenger)
        {
            IndividualPassenger = passenger ?? throw new ArgumentNullException(nameof(passenger));
            FamilyGroup = null;
        }

        // Constructor for family group
        public BoardingGroup(Family family)
        {
            FamilyGroup = family ?? throw new ArgumentNullException(nameof(family));
            IndividualPassenger = null;
        }

        // TODO: Consider if any other methods are needed.
    }
} 