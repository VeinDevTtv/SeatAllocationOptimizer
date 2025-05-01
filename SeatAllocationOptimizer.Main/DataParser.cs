using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization; // For parsing double

namespace SeatAllocationOptimizer.Main
{
    public static class DataParser
    {
        // Parses the input CSV file (format: ID,Type,Revenue,FamilyID,WindowPref)
        public static List<BoardingGroup> ParseInput(string filePath)
        {
            Console.WriteLine($"Parsing input from: {filePath}");
            var passengersByFamily = new Dictionary<string, List<Passenger>>();
            var individualPassengers = new List<Passenger>();

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    int lineNumber = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

                        string[] parts = line.Split(',');
                        if (parts.Length < 5) // Expecting at least 5 parts now (including window pref)
                        {
                            Console.WriteLine($"Warning: Skipping malformed line {lineNumber} (expected 5+ parts): {line}");
                            continue;
                        }

                        try
                        {
                            // Parse basic info
                            // int id = int.Parse(parts[0].Trim()); // ID not strictly needed for allocation logic yet
                            bool isAdult = parts[1].Trim().Equals("Adult", StringComparison.OrdinalIgnoreCase);
                            double revenue = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                            string familyId = parts[3].Trim();
                            int seatsNeeded = 1; // Assuming 1 seat per passenger based on current classes
                            bool wantsWindow = parts[4].Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase);
                            // TODO: Consider if input format could specify >1 seat needed per passenger

                            var passenger = new Passenger(isAdult, revenue, seatsNeeded, wantsWindow);

                            if (familyId == "-")
                            {
                                individualPassengers.Add(passenger);
                            }
                            else
                            {
                                if (!passengersByFamily.ContainsKey(familyId))
                                {
                                    passengersByFamily[familyId] = new List<Passenger>();
                                }
                                passengersByFamily[familyId].Add(passenger);
                            }
                        }
                        catch (FormatException ex)
                        {
                            Console.WriteLine($"Warning: Skipping line {lineNumber} due to parsing error ({ex.Message}): {line}");
                        }
                        catch (Exception ex) // Catch other potential errors per line
                        {
                             Console.WriteLine($"Warning: Skipping line {lineNumber} due to unexpected error ({ex.Message}): {line}");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: Input file not found at {filePath}");
                return new List<BoardingGroup>(); // Return empty list
            }
             catch (IOException ex)
            {
                Console.WriteLine($"Error: Could not read input file {filePath}. {ex.Message}");
                return new List<BoardingGroup>(); // Return empty list
            }


            // Create BoardingGroup objects
            var boardingGroups = new List<BoardingGroup>();

            // Add individuals
            foreach (var individual in individualPassengers)
            {
                boardingGroups.Add(new BoardingGroup(individual));
            }

            // Add families
            foreach (var kvp in passengersByFamily)
            {
                var family = new Family(kvp.Key); // Pass familyId to constructor
                foreach (var member in kvp.Value)
                {
                    family.AddMember(member);
                }
                 if(family.Members.Any())
                 {
                    boardingGroups.Add(new BoardingGroup(family));
                 }
            }

            Console.WriteLine($"Parsed {boardingGroups.Count} boarding groups ({individualPassengers.Count} individuals, {passengersByFamily.Count} families).");
            return boardingGroups;
        }
    }
} 