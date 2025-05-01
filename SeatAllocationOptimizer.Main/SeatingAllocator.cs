using System;
using System.Collections.Generic;
using System.Linq;

namespace SeatAllocationOptimizer.Main
{
    public class SeatingAllocator
    {
        private readonly int _planeRows;
        private readonly int _seatsPerRow;
        private BoardingGroup?[,] _seatMap; // Using nullable BoardingGroup to store which group is in which seat

        public SeatingAllocator(int planeRows = 4, int seatsPerRow = 5) // Default based on README initial config (20 seats / 4 rows)
        {
            // README mentions 20 seats across 4 rows initially, but also planeRows = 33 for 200 seats.
            // Let's make it configurable. Assuming 200 seats might mean 33 rows * 6 seats/row + 2 extra? Or maybe 40*5? Let's stick to configurable.
             if (planeRows <= 0) throw new ArgumentOutOfRangeException(nameof(planeRows), "Plane must have at least one row.");
             if (seatsPerRow <= 0) throw new ArgumentOutOfRangeException(nameof(seatsPerRow), "Rows must have at least one seat.");

            _planeRows = planeRows;
            _seatsPerRow = seatsPerRow;
            _seatMap = new BoardingGroup?[_planeRows, _seatsPerRow];
        }

        // Represents the result of the allocation
        public class AllocationResult
        {
            public BoardingGroup?[,] SeatMap { get; }
            public double TotalRevenue { get; }
            public List<BoardingGroup> UnseatedGroups { get; }

            public AllocationResult(BoardingGroup?[,] seatMap, double totalRevenue, List<BoardingGroup> unseatedGroups)
            {
                SeatMap = seatMap;
                TotalRevenue = totalRevenue;
                UnseatedGroups = unseatedGroups;
            }
        }


        // Corresponds to the idealRevenue method description
        public AllocationResult AllocateSeats(List<BoardingGroup> initialGroups)
        {
            _seatMap = new BoardingGroup?[_planeRows, _seatsPerRow]; // Reset map for new allocation
            var boardingQueue = new PriorityQueue<BoardingGroup, BoardingGroup>(new BoardingGroupComparator());
            foreach (var group in initialGroups)
            {
                boardingQueue.Enqueue(group, group);
            }

            int currentRow = 0;
            int currentSeat = 0;
            int totalSeatedPassengersCount = 0; // To track total passengers, not just groups
            List<BoardingGroup> temporarilyUnseated = new List<BoardingGroup>();

            // Loop until the main queue is empty and no more progress can be made in a cycle
            while (boardingQueue.Count > 0)
            {
                int passengersSeatedThisCycle = 0;

                // Process the current queue
                while (boardingQueue.TryDequeue(out BoardingGroup? currentGroup, out _))
                {
                     if (currentGroup == null) continue; // Should not happen with non-null enqueue, but safety check

                    bool seated = TrySeatGroup(currentGroup, ref currentRow, ref currentSeat);

                    if (seated)
                    {
                        passengersSeatedThisCycle += currentGroup.SeatsNeeded;
                    }
                    else
                    {
                        // Could not seat group in the current state, hold it back
                        temporarilyUnseated.Add(currentGroup);
                    }
                }

                // After trying to seat everyone once, re-enqueue those held back
                if (temporarilyUnseated.Any())
                {
                    // If no one was seated in this cycle, and we still have people waiting,
                    // advance to the next row to potentially find space.
                    // README: "If no new passengers were seated during a round, the seating process advances to the next row."
                    if (passengersSeatedThisCycle == 0 && currentRow < _planeRows - 1)
                    {
                         Console.WriteLine($"No progress made in row {currentRow}, advancing to next row.");
                        currentRow++;
                        currentSeat = 0; // Start at the beginning of the new row
                    }

                    // Re-enqueue for the next attempt cycle
                    foreach(var group in temporarilyUnseated)
                    {
                         boardingQueue.Enqueue(group, group);
                    }
                    temporarilyUnseated.Clear();

                     // Avoid infinite loops if we keep advancing rows but can't seat anyone
                     if (passengersSeatedThisCycle == 0 && currentRow >= _planeRows - 1 && boardingQueue.Count > 0) {
                         Console.WriteLine("Warning: Cannot seat remaining groups. Plane might be full or groups too large for remaining spaces.");
                         break; // Exit main loop if stuck
                     }
                }
                // If temporarilyUnseated is empty, it means everyone from the last cycle got seated or the initial queue was empty.
                // The main while loop condition (boardingQueue.Count > 0) handles exit.

                totalSeatedPassengersCount += passengersSeatedThisCycle;
            }


            // Calculate final revenue from the map
            double totalRevenue = CalculateRevenueFromMap();

            // Collect any groups that remained in the queue (e.g., if we broke the loop early)
             List<BoardingGroup> finalUnseated = new List<BoardingGroup>();
             while(boardingQueue.TryDequeue(out BoardingGroup? group, out _)) {
                 if (group != null) finalUnseated.Add(group);
             }
             finalUnseated.AddRange(temporarilyUnseated); // Add any leftover from the last cycle

             Console.WriteLine($"Allocation complete. Total Revenue: {totalRevenue:C}");
             if(finalUnseated.Any()) {
                Console.WriteLine($"Could not seat {finalUnseated.Sum(g => g.SeatsNeeded)} passengers from {finalUnseated.Count} groups.");
             }


            return new AllocationResult(_seatMap, totalRevenue, finalUnseated);
        }

        // Attempts to seat a group starting from the current position
        private bool TrySeatGroup(BoardingGroup group, ref int startRow, ref int startSeat)
        {
            int seatsNeeded = group.SeatsNeeded;

             // Iterate through rows starting from startRow
            for (int r = startRow; r < _planeRows; r++)
            {
                // Determine starting seat for this row
                int s = (r == startRow) ? startSeat : 0;

                // Check remaining seats in the current row
                while (s <= _seatsPerRow - seatsNeeded)
                {
                    // Check if the block of seats is available
                    bool blockAvailable = true;
                    for (int i = 0; i < seatsNeeded; i++)
                    {
                        if (_seatMap[r, s + i] != null)
                        {
                            blockAvailable = false;
                            break;
                        }
                    }

                    if (blockAvailable)
                    {
                        // Additional check for family groups: ensure no child is isolated
                        if (group.IsFamily && group.FamilyGroup!.HasChildren) {
                            if (!IsFamilyPlacementValid(group.FamilyGroup, r, s, seatsNeeded)) {
                                // Invalid placement according to family rules, treat block as unavailable
                                blockAvailable = false;
                                // Continue searching from the next seat (s++ below)
                            } 
                        }
                    }

                    // Place the group if block is still available
                    if (blockAvailable)
                    {
                        // Place the group
                        for (int i = 0; i < seatsNeeded; i++)
                        {
                            _seatMap[r, s + i] = group;
                        }

                        // Update the global position for the *next* attempt
                        startRow = r;
                        startSeat = s + seatsNeeded;
                        // Handle moving to next row if current is filled
                         if (startSeat >= _seatsPerRow) {
                            startRow++;
                            startSeat = 0;
                         }

                        return true; // Group seated successfully
                    }
                    else
                    {
                        // Move to the next possible starting seat in this row
                        s++;
                    }
                }
                 // If we finish checking row 'r' and the group wasn't seated,
                 // and if we started checking this row from the beginning (s=0),
                 // no need to re-check this row in future calls for *this specific group search*.
                 // The outer loop will move to the next row (r++).
                 // If r == startRow, it means we started mid-row, so we must continue to the next row.
            }

            // If we've checked all rows from startRow onwards and couldn't seat the group
            return false;
        }

        // Helper to check if a potential family placement keeps children adjacent to adults
        private bool IsFamilyPlacementValid(Family family, int row, int startSeat, int seatsNeeded)
        {
            // Simplistic check: Iterate through the seats assigned to this family.
            // If a seat holds a child, check if at least one adjacent seat (within the block) holds an adult from the same family.
            // Note: This assumes the family members list order maps directly to seating order, which isn't guaranteed by the current placement.
            // A more robust check would need to map specific members to specific seats [r, s+i].
            // For now, let's assume a basic check: does *any* child lack *any* adjacent adult within the block?

            List<Passenger> members = family.Members;
            if (members.Count != seatsNeeded) {
                 // This implies SeatsNeeded calculation might be different from member count - handle defensively
                 // For now, skip the check if counts don't match, as seating assignment is unclear.
                 Console.WriteLine($"Warning: Skipping family validity check for {family.FamilyId} - member count mismatch.");
                 return true; 
            }

            bool hasAdult = members.Any(p => p.IsAdult);
            if (!hasAdult && family.HasChildren) {
                Console.WriteLine($"Warning: Cannot seat family {family.FamilyId} - contains children but no adults.");
                return false; // Cannot satisfy adjacency if no adults exist
            }

            for (int i = 0; i < seatsNeeded; i++)
            {
                Passenger currentPassenger = members[i]; // Assumption: members[i] goes to seat startSeat + i
                if (!currentPassenger.IsAdult)
                {
                    // Check left neighbor (if within block)
                    bool adultNeighborFound = false;
                    if (i > 0 && members[i - 1].IsAdult)
                    {
                        adultNeighborFound = true;
                    }
                    // Check right neighbor (if within block)
                    if (!adultNeighborFound && i < seatsNeeded - 1 && members[i + 1].IsAdult)
                    {
                        adultNeighborFound = true;
                    }

                    if (!adultNeighborFound)
                    {
                        Console.WriteLine($"Debug: Invalid placement for family {family.FamilyId} at [{row},{startSeat}] - child at index {i} has no adjacent adult within the block.");
                        return false; // Found an isolated child
                    }
                }
            }

            return true; // All children have an adjacent adult within the block
        }

        private double CalculateRevenueFromMap()
        {
            double totalRevenue = 0;
            HashSet<BoardingGroup> countedGroups = new HashSet<BoardingGroup>(); // Avoid double-counting revenue for groups spanning multiple seats

            for (int r = 0; r < _planeRows; r++)
            {
                for (int s = 0; s < _seatsPerRow; s++)
                {
                    BoardingGroup? group = _seatMap[r, s];
                    if (group != null && countedGroups.Add(group)) // Add returns true if item was added (i.e., not already present)
                    {
                         totalRevenue += group.GroupRevenue;
                    }
                }
            }
            return totalRevenue;
        }

        // Optional: Method to visualize the seating map (can be moved to a separate class later)
        public void PrintSeatingMap()
        {
            Console.WriteLine("\n--- Seating Map ---");
            for (int r = 0; r < _planeRows; r++)
            {
                 Console.Write($"Row {r+1}: [");
                for (int s = 0; s < _seatsPerRow; s++)
                {
                    BoardingGroup? group = _seatMap[r, s];
                     string seatDisplay;
                     if (group == null) {
                         seatDisplay = " --- ";
                     } else {
                         // Use the GroupId for display
                         // Pad or truncate for consistent width (e.g., 5 chars)
                         string displayId = group.GroupId.Length > 5 ? group.GroupId.Substring(0, 5) : group.GroupId.PadRight(5);
                         seatDisplay = $" {displayId} ";
                     }
                     Console.Write(seatDisplay);
                     if (s < _seatsPerRow - 1) Console.Write("|");
                }
                Console.WriteLine("]");
            }
            Console.WriteLine("-------------------\n");
        }
    }
} 