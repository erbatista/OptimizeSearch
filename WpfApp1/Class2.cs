using System;
using System.Threading;
using System.Threading.Tasks;

public class ClassA
{
    private bool _isBusy = false;

    // The "Waiting Room" for the SINGLE pending item.
    private TaskCompletionSource<bool> _pendingJobTicket;

    // The Lock for thread safety
    private readonly object _lock = new object();

    public bool AwaitingEvent => _isBusy;

    public async Task Run()
    {
        TaskCompletionSource<bool> myTicket = null;
        bool runImmediately = false;

        // --- PHASE 1: Try to Enter ---
        lock (_lock)
        {
            if (!_isBusy)
            {
                // CASE 1: Not busy. Run immediately.
                _isBusy = true;
                runImmediately = true;
            }
            else
            {
                // CASE 2: Busy. Check Queue.
                if (_pendingJobTicket != null)
                {
                    // Rule: Only one pending call. 
                    // If queue is full, IGNORE/REJECT this call.
                    return;
                }

                // Rule: Enqueue this call.
                _pendingJobTicket = new TaskCompletionSource<bool>();
                myTicket = _pendingJobTicket;
            }
        }

        // --- PHASE 2: Execution or Wait ---

        if (runImmediately)
        {
            await someService.RemoteWorkAsync();
            // We do NOT set _isBusy = false here. 
            // We wait for SomeEventHandler to do it.
        }
        else if (myTicket != null)
        {
            // We are queued. Wait for Signal OR 3s Timeout.
            var completedTask = await Task.WhenAny(myTicket.Task, Task.Delay(3000));

            if (completedTask == myTicket.Task)
            {
                // --- WAKE UP LOGIC ---
                // We were signaled because SomeEventHandler finished the previous job
                // and set _isBusy = false.

                // CRITICAL: We must set _isBusy = true again immediately 
                // because we are about to start running.
                lock (_lock)
                {
                    _isBusy = true;
                }

                await someService.RemoteWorkAsync();
            }
            else
            {
                // --- TIMEOUT LOGIC ---
                HandleTimeout();
            }
        }
    }

    public void SomeEventHandler()
    {
        // 1. Do the action
        SomeAction();

        lock (_lock)
        {
            // 2. ALWAYS set flag to false (as per your requirement)
            _isBusy = false;

            // 3. Check if someone is waiting to run
            if (_pendingJobTicket != null)
            {
                var queuedJob = _pendingJobTicket;
                _pendingJobTicket = null; // Remove from queue

                // 4. Trigger the queued job.
                // It will wake up, re-lock, set _isBusy=true, and run.
                queuedJob.TrySetResult(true);
            }
        }
    }

    private void HandleTimeout()
    {
        lock (_lock)
        {
            // 1. Remove from queue (if we are still there)
            if (_pendingJobTicket != null)
            {
                _pendingJobTicket = null;
            }

            // 2. Reset flag (Safety mechanism)
            // If the previous job is stuck forever, this allows the system to recover.
            _isBusy = false;
        }
    }
}



/*
public void ClientClass
{
private ClassA _classA;

async Task SomeEventHandler_FromAnotherThread()
{
    // Simply await. 
    // If queued, this await will pause for up to 3 seconds.
    // If rejected, it returns immediately.
    await _classA.Run();
}  
}






"Maintain a maximum of 10 active UI-related bugs in the [Project Name] backlog throughout the 2026 fiscal year. This will be achieved by triaging new UI issues within 48 hours and dedicating a minimum of 4 hours per sprint specifically to UI debt and front-end bug resolution."




"Reduce the application 'Cold Start' time by 20% for the [Main Project Name] by the end of Q3 2026. This will be achieved by auditing Resource Dictionary loading, implementing UI Virtualization on primary data grids, and utilizing performance profiling tools to identify and resolve UI thread bottlenecks."



"Increase codebase reliability and maintainability by integrating specific C# 10 features—specifically Record types for data models and Extended Property Patterns for UI logic—into all new feature development and 100% of bug-fix refactors throughout 2026. This initiative aims to reduce 'avoidable' logic bugs by at least 15% through improved immutability and clearer conditional flows."


"Develop and deploy an internal WPF-based 'NLog Visualizer' tool by the end of Q2 2026. This tool will provide real-time filtering, color-coded severity levels, and exception stack-trace parsing for local and network log files, aiming to reduce the time spent on manual log analysis during bug investigations by 30%."


I’m writing to discuss my current in-office schedule. I remain fully committed to my role and being present with the team, but I am currently facing a logistical constraint regarding my commute.

My wife and I share a single vehicle, which makes being in the office five days a week difficult to sustain logistically. To ensure I can maintain punctuality and reliability, I would like to propose a hybrid schedule where I am in the office [Number, e.g., 3] days a week (e.g., [Days, e.g., Tuesday, Wednesday, Thursday]).

I am confident that I can maintain full productivity and collaboration on the days I am working remotely. Please let me know if this arrangement would be acceptable or if we can find a time to discuss it further.



using System.Collections.Generic;

public class WifiChannelProcessor
{
    // Configuration constants for Grid Layout
    private const int GridRowSize = 9;

    public void GenerateChannels(int minChannel, int maxChannel)
    {
        var channels = new List<int>();
        var leftChannels = new List<int>();   // Start of the row
        var rightChannels = new List<int>();  // End of the row

        // NOTE: Keeping original logic of starting at Min + 1. 
        // Be aware this skips Channel 1 if MinChannel is 1.
        for (int ch = minChannel + 1; ch <= maxChannel; ch++)
        {
            if (IsValidWifiChannel(ch))
            {
                ProcessChannelForGrid(ch, channels, leftChannels, rightChannels);
            }
        }
    }

    /// <summary>
    /// Determines if a number represents a valid standard WiFi Channel 
    /// across 2.4GHz and 5GHz bands.
    /// </summary>
    private bool IsValidWifiChannel(int ch)
    {
        // 2.4 GHz Band (Channels 1-14)
        bool is24Ghz = ch <= 14;

        // 5 GHz UNII-1 (34-48). Original code allows even numbers (step 2).
        // Standard implies step 4, but we preserve legacy logic here.
        bool isLow5Ghz = (ch >= 34 && ch <= 48) && (ch % 2 == 0);

        // 5 GHz UNII-2 (52-64). Step 4.
        bool isMid5Ghz = (ch >= 52 && ch <= 64) && (ch % 4 == 0);

        // 5 GHz UNII-2 Extended (100-140). Step 4.
        bool isExt5Ghz = (ch >= 100 && ch <= 140) && (ch % 4 == 0);

        // 5 GHz UNII-3 / Upper (149-165). Step 4, starting at 149.
        bool isHigh5Ghz = (ch >= 149) && (ch % 4 == 1);

        return is24Ghz || isLow5Ghz || isMid5Ghz || isExt5Ghz || isHigh5Ghz;
    }

    /// <summary>
    /// Sorts the channel into the appropriate list (Left, Right, or Standard)
    /// based on the current grid row position.
    /// </summary>
    private void ProcessChannelForGrid(int ch, List<int> allChannels, List<int> left, List<int> right)
    {
        int currentCount = allChannels.Count;

        // Determine position in the row (0 to 8)
        int positionInRow = currentCount % GridRowSize;

        // Logic Mapping:
        // Position 0 = First item in row -> Left List
        // Position 8 = Last item in row  -> Right List
        
        if (positionInRow == (GridRowSize - 1)) // Index 8
        {
            right.Add(ch);
        }
        else if (positionInRow == 0) // Index 0
        {
            left.Add(ch);
        }

        // Always add to the master list
        allChannels.Add(ch);
    }
}



unit test



using NUnit.Framework; // or Xunit
   using System.Collections.Generic;
   using System.Linq;
   
   [TestFixture]
   public class WifiChannelTests
   {
       [Test]
       public void Verify_Refactor_Preserves_Legacy_Behavior()
       {
           // ARRANGE
           int minChannel = 1;
           int maxChannel = 165;
   
           // 1. Run the Old "Spaghetti" Logic
           var legacy = new LegacyWifiGenerator();
           legacy.Run(minChannel, maxChannel);
   
           // 2. Run the New "Clean" Logic
           var modern = new WifiChannelProcessor(); // The class from my previous answer
           var modernResults = modern.GenerateChannels(minChannel, maxChannel);
   
           // ASSERT - Compare the lists item by item
           
           // Check the Main "All Channels" list
           CollectionAssert.AreEqual(legacy.Channels, modernResults.AllChannels, 
               "The main list of generated channels differs!");
   
           // Check the "Left" (Start of Row) list
           CollectionAssert.AreEqual(legacy.LeftChannels, modernResults.LeftChannels, 
               "The Left/Start-of-row logic is broken.");
   
           // Check the "Right" (End of Row) list
           CollectionAssert.AreEqual(legacy.RightChannels, modernResults.RightChannels, 
               "The Right/End-of-row logic is broken.");
       }
   }
   
   // ==========================================
   // MOCK CLASSES FOR THE TEST CONTEXT
   // ==========================================
   
   // 1. The Original Logic (Recreated exactly from your image)
   public class LegacyWifiGenerator
   {
       public List<int> Channels { get; } = new List<int>();
       public List<int> LeftChannels { get; } = new List<int>();
       public List<int> RightChannels { get; } = new List<int>();
   
       public void Run(int MinChannel, int MaxChannel)
       {
           for (var channel = MinChannel + 1; channel <= MaxChannel; channel++)
           {
               if (channel <= 14 ||
                  (channel >= 34 && channel <= 48 && channel % 2 == 0) ||
                  (channel >= 52 && channel <= 64 && channel % 4 == 0) ||
                  (channel >= 100 && channel <= 140 && channel % 4 == 0) ||
                  (channel >= 149 && channel % 4 == 1))
               {
                   if (Channels.Count % 9 == 8)
                   {
                       Channels.Add(channel);
                       RightChannels.Add(channel);
                   }
                   else if (Channels.Count % 9 == 0)
                   {
                       LeftChannels.Add(channel);
                       Channels.Add(channel);
                   }
                   else
                   {
                       Channels.Add(channel);
                   }
               }
           }
       }
   }
   
   // 2. The Refactored Logic (Slightly adjusted to return the lists for testing)
   public class WifiChannelProcessor
   {
       public (List<int> AllChannels, List<int> LeftChannels, List<int> RightChannels) GenerateChannels(int min, int max)
       {
           var channels = new List<int>();
           var left = new List<int>();
           var right = new List<int>();
   
           for (int ch = min + 1; ch <= max; ch++)
           {
               if (IsValidWifiChannel(ch))
               {
                   // Logic derived from my previous refactor
                   int positionInRow = channels.Count % 9;
   
                   if (positionInRow == 0) left.Add(ch);
                   
                   // Add to main list
                   channels.Add(ch);
   
                   // Note: The original code added to Right list AFTER adding to main list
                   // if the count WAS 8 (meaning index 8, the 9th item). 
                   // However, strictly looking at the image:
                   // if (count % 9 == 8) { add to main; add to right; }
                   // So checking BEFORE addition is correct.
                   if (positionInRow == 8) right.Add(ch);
               }
           }
           return (channels, left, right);
       }
   
       private bool IsValidWifiChannel(int ch)
       {
           bool is24Ghz = ch <= 14;
           bool isLow5Ghz = (ch >= 34 && ch <= 48) && (ch % 2 == 0);
           bool isMid5Ghz = (ch >= 52 && ch <= 64) && (ch % 4 == 0);
           bool isExt5Ghz = (ch >= 100 && ch <= 140) && (ch % 4 == 0);
           bool isHigh5Ghz = (ch >= 149) && (ch % 4 == 1);
   
           return is24Ghz || isLow5Ghz || isMid5Ghz || isExt5Ghz || isHigh5Ghz;
       }
   }


// "record" keyword gives you Immutability + Value Equality for free
   public record Channel : IComparable<Channel>
   {
       public BandType Band { get; }
       public uint Number { get; }
   
       public Channel(BandType band, uint number)
       {
           // Guard Clauses: Protect your domain logic
           if (number == 0) 
               throw new ArgumentOutOfRangeException(nameof(number), "Channel 0 is invalid.");
           
           // Optional: Add specific logic (e.g., 2.4GHz must be <= 14)
           if (band == BandType.GHz24 && number > 14)
                throw new ArgumentOutOfRangeException(nameof(number), "Invalid 2.4GHz channel.");
   
           Band = band;
           Number = number;
       }
   
       // Implementing IComparable allows you to use .Sort() or OrderBy() easily
       public int CompareTo(Channel? other)
       {
           if (other is null) return 1;
           
           // Sort by Band first, then by Number
           int bandComparison = Band.CompareTo(other.Band);
           if (bandComparison != 0) return bandComparison;
           
           return Number.CompareTo(other.Number);
       }
       
       // Nice to have: Override ToString for debugging/logging
       public override string ToString() => $"{Band} - Ch {Number}";
   }


public record Channel : IComparable<Channel>
   {
       public BandType Band { get; }
       public uint Number { get; }
   
       // Primary Constructor
       public Channel(BandType band, uint number)
       {
           // 1. Universal Guard Clause
           if (number == 0) 
               throw new ArgumentOutOfRangeException(nameof(number), "Channel 0 is never valid.");
   
           // 2. Conditional Validation (The "Smart" Logic)
           // Only enforce strict band rules if we are NOT in Legacy mode.
           if (band != BandType.Legacy)
           {
                if (band == BandType.Band2_4GHz && number > 14)
                    throw new ArgumentOutOfRangeException(nameof(number), "Invalid 2.4GHz channel.");
                
                // Add other band-specific rules here
           }
   
           Band = band;
           Number = number;
       }
   
       // ==========================================
       // THE FACTORY METHOD (The Senior Dev Solution)
       // ==========================================
       /// <summary>
       /// Creates a channel for legacy devices that do not report frequency band.
       /// Usage: var ch = Channel.CreateLegacy(6);
       /// </summary>
       public static Channel CreateLegacy(uint number)
       {
           return new Channel(BandType.Legacy, number);
       }
   
       // Update ToString for cleaner UI
       public override string ToString()
       {
           if (Band == BandType.Legacy)
               return $"Legacy Ch {Number}"; // Distinct display for weird channels
               
           return $"{Band.ToFriendlyString()} - Ch {Number}";
       }
   
       // Update Sorting to push Legacy items to the bottom (or top)
       public int CompareTo(Channel? other)
       {
           if (other is null) return 1;
   
           // Group by Band first
           int bandComparison = Band.CompareTo(other.Band);
           if (bandComparison != 0) return bandComparison;
   
           // Then by Number
           return Number.CompareTo(other.Number);
       }

public static string ToFriendlyString(this BandType band)
    {
        return band switch
        {
            BandType.Legacy     => "Legacy / No Band",
            BandType.Band2_4GHz => "2.4 GHz",
            BandType.Band5GHz   => "5 GHz",
            BandType.Band6GHz   => "6 GHz",
            BandType.Unknown    => "Unknown",
            _                   => band.ToString() // Fallback for undefined values
        };
    }
   }

*/

/*
 using System.Collections.Generic;
   using System.Linq;
   
   // Using the Record definition we created earlier
   // Ensure you have the BandType enum updated with Band6GHz
   public class ChannelRepository : IChannelSelectionData
   {
       // Public Properties
       public IReadOnlyList<Channel> LowBandChannels { get; }
       public IReadOnlyList<Channel> HighBandChannels { get; }
       public IReadOnlyList<Channel> SixGhzChannels { get; } // New!
       public IReadOnlyList<Channel> AllChannels { get; }
   
       // Grid System (Left/Right edge detection)
       // We use HashSet for O(1) lookups. 
       // Since Channel is a record, equality checks work automatically.
       private readonly HashSet<Channel> _leftChannels = new();
       private readonly HashSet<Channel> _rightChannels = new();
   
       public IReadOnlyCollection<Channel> LeftChannels => _leftChannels;
       public IReadOnlyCollection<Channel> RightChannels => _rightChannels;
   
       private const int GridRowSize = 9;
   
       public ChannelRepository()
       {
           // 1. Generate 2.4 GHz (1 - 14)
           LowBandChannels = GenerateBand(BandType.Band2_4GHz, 1, 14, step: 1);
   
           // 2. Generate 5 GHz (Complex Logic)
           HighBandChannels = Generate5GhzChannels();
   
           // 3. Generate 6 GHz (1 - 233, Step 4)
           // Standard 6GHz starts at 1 and increments by 4 (1, 5, 9, ... 233)
           SixGhzChannels = GenerateBand(BandType.Band6GHz, 1, 233, step: 4);
   
           // 4. Combine all into master list
           AllChannels = LowBandChannels
               .Concat(HighBandChannels)
               .Concat(SixGhzChannels)
               .ToList();
       }
   
       /// <summary>
       /// Generic generator for simple stepped bands (2.4GHz and 6GHz)
       /// </summary>
       private IReadOnlyList<Channel> GenerateBand(BandType band, uint min, uint max, uint step)
       {
           var list = new List<Channel>();
           
           for (uint i = min; i <= max; i += step)
           {
               var ch = new Channel(band, i);
               list.Add(ch);
               
               // Apply Grid Logic for this specific band
               CalculateGridPosition(ch, list.Count); 
           }
           return list;
       }
   
       /// <summary>
       /// Specific generator for 5GHz because of its irregular gaps and DFS ranges
       /// </summary>
       private IReadOnlyList<Channel> Generate5GhzChannels()
       {
           var list = new List<Channel>();
           
           // Use the validated logic we refactored previously
           // Iterating 36 to 165
           for (uint i = 36; i <= 165; i++)
           {
               // Logic: Is this a valid 5GHz channel?
               bool isValid = 
                   (i >= 36 && i <= 48 && i % 4 == 0) ||   // UNII-1
                   (i >= 52 && i <= 64 && i % 4 == 0) ||   // UNII-2
                   (i >= 100 && i <= 144 && i % 4 == 0) || // UNII-2e (Including 144)
                   (i >= 149 && i <= 165 && i % 4 == 1);   // UNII-3
   
               if (isValid)
               {
                   var ch = new Channel(BandType.Band5GHz, i);
                   list.Add(ch);
                   CalculateGridPosition(ch, list.Count);
               }
           }
           return list;
       }
   
       /// <summary>
       /// Centralized Logic for "Row of 9" calculation.
       /// Uses 'index' (1-based count) to determine position.
       /// </summary>
       private void CalculateGridPosition(Channel ch, int currentCountInBand)
       {
           // currentCountInBand is 1-based (1, 2, 3...)
           // Modulo math to find edges
           
           if (currentCountInBand % GridRowSize == 1) 
           {
               // First item in a row (Index 0, 9, 18...)
               _leftChannels.Add(ch);
           }
           else if (currentCountInBand % GridRowSize == 0)
           {
               // Last item in a row (Index 8, 17, 26...)
               _rightChannels.Add(ch);
           }
       }
   
       // This replaces your old 'GetAvailableChannels'
       public IEnumerable<SurveyChannelVm> GetAvailableChannels()
       {
           // Now works safely because 'Channel' record handles equality correctly
           return AllChannels.Select(ch => new SurveyChannelVm(
               ch, 
               false, 
               _leftChannels.Contains(ch), 
               _rightChannels.Contains(ch)
           ));
       }
   }
 */

/*
 public interface IChannelSelectionData
   {
       #region Properties
   
       /// <summary>
       /// Legacy alias for 2.4 GHz Channels.
       /// </summary>
       IReadOnlyList<Channel> LowBandChannels { get; }
   
       /// <summary>
       /// Legacy alias for 5 GHz Channels.
       /// </summary>
       IReadOnlyList<Channel> HighBandChannels { get; }
   
       /// <summary>
       /// NEW: 6 GHz Channels (Wi-Fi 6E/7).
       /// </summary>
       IReadOnlyList<Channel> SixGhzChannels { get; }
   
       /// <summary>
       /// Master list containing all channels from all bands.
       /// </summary>
       IReadOnlyList<Channel> Channels { get; }
   
       // UI Grid Helpers
       IReadOnlyCollection<Channel> LeftChannels { get; }
       IReadOnlyCollection<Channel> RightChannels { get; }
   
       #endregion
   
       #region Methods
       IEnumerable<SurveyChannelVm> GetAvailableChannels();
       #endregion
   }

------------------------
using System.Collections.Generic;
   using System.Linq;
   
   public class ChannelSelectionData : IChannelSelectionData
   {
       // ==========================================
       // 1. Properties from Interface
       // ==========================================
       public IReadOnlyList<Channel> LowBandChannels { get; }
       public IReadOnlyList<Channel> HighBandChannels { get; }
       public IReadOnlyList<Channel> SixGhzChannels { get; } // New 6GHz support
       public IReadOnlyList<Channel> Channels { get; }       // The master list
   
       // ==========================================
       // 2. Internal Grid State (Left/Right logic)
       // ==========================================
       // NOTE: Requires 'Channel' to implement Equals/GetHashCode or be a 'record'
       private readonly HashSet<Channel> _leftChannels = new();
       private readonly HashSet<Channel> _rightChannels = new();
   
       public IReadOnlyCollection<Channel> LeftChannels => _leftChannels;
       public IReadOnlyCollection<Channel> RightChannels => _rightChannels;
   
       private const int GridRowSize = 9;
   
       // ==========================================
       // 3. Constructor (Data Generation)
       // ==========================================
       public ChannelSelectionData()
       {
           // Generate 2.4 GHz (Channels 1-14)
           LowBandChannels = GenerateSteppedBand(BandType.Band2_4GHz, 1, 14, 1);
   
           // Generate 5 GHz (Complex standard logic)
           HighBandChannels = Generate5GhzBand();
   
           // Generate 6 GHz (Channels 1-233, Step 4)
           SixGhzChannels = GenerateSteppedBand(BandType.Band6GHz, 1, 233, 4);
   
           // Combine all into the master list
           Channels = LowBandChannels
               .Concat(HighBandChannels)
               .Concat(SixGhzChannels)
               .ToList();
       }
   
       // ==========================================
       // 4. Private Helper Methods
       // ==========================================
   
       /// <summary>
       /// Generates channels for regular bands like 2.4GHz and 6GHz.
       /// </summary>
       private IReadOnlyList<Channel> GenerateSteppedBand(BandType band, uint min, uint max, uint step)
       {
           var list = new List<Channel>();
           // Using strict '<=' to ensure the last channel is included
           for (uint i = min; i <= max; i += step)
           {
               var ch = new Channel(band, i);
               list.Add(ch);
               CalculateGridPosition(ch, list.Count);
           }
           return list;
       }
   
       /// <summary>
       /// Generates 5GHz channels based on standard UNII rules.
       /// </summary>
       private IReadOnlyList<Channel> Generate5GhzBand()
       {
           var list = new List<Channel>();
   
           // Range 36 to 165 covers all standard 5GHz channels
           for (uint i = 36; i <= 165; i++)
           {
               // Valid if:
               // UNII-1 (36-48), UNII-2 (52-64), UNII-2e (100-144), UNII-3 (149-165)
               // Note: UNII-3 starts at 149 and steps by 4 (i % 4 == 1)
               bool isValid =
                   (i >= 36 && i <= 48 && i % 4 == 0) ||
                   (i >= 52 && i <= 64 && i % 4 == 0) ||
                   (i >= 100 && i <= 144 && i % 4 == 0) ||
                   (i >= 149 && i <= 165 && i % 4 == 1);
   
               if (isValid)
               {
                   var ch = new Channel(BandType.Band5GHz, i);
                   list.Add(ch);
                   CalculateGridPosition(ch, list.Count);
               }
           }
           return list;
       }
   
       /// <summary>
       /// Determines if a channel is the start (Left) or end (Right) of a UI row.
       /// </summary>
       private void CalculateGridPosition(Channel ch, int indexInBand)
       {
           // 1-based index from list.Count
           if (indexInBand % GridRowSize == 1)
           {
               _leftChannels.Add(ch);
           }
           else if (indexInBand % GridRowSize == 0)
           {
               _rightChannels.Add(ch);
           }
       }
   
       // ==========================================
       // 5. Public Method Implementation
       // ==========================================
       public IEnumerable<SurveyChannelVm> GetAvailableChannels()
       {
           return Channels.Select(ch => new SurveyChannelVm(
               ch,
               false, // IsSelected (Default)
               _leftChannels.Contains(ch),
               _rightChannels.Contains(ch)
           ));
       }
   }


 */

/*
 /// <summary>
   /// Generates 5GHz channels using the LEGACY logic (Step 2) to support 
   /// older devices or non-standard frequencies (e.g., Ch 34, 38, 42).
   /// </summary>
   private IReadOnlyList<Channel> Generate5GhzBand()
   {
       var list = new List<Channel>();

       // RESTORED LEGACY RANGE: 
       // Original code checked (channel >= 34).
       // Original code checked (channel % 2 == 0) for the lower band.
       
       for (uint i = 34; i <= 165; i++)
       {
           bool isValid = false;

           // 1. Lower Band (UNII-1 & extensions)
           // LEGACY BEHAVIOR: 34 - 48, Step 2 (Even numbers: 34, 36, 38... 48)
           if (i >= 34 && i <= 48 && i % 2 == 0)
           {
               isValid = true;
           }
           // 2. Mid Band (UNII-2)
           // Standard Behavior: 52 - 64, Step 4
           else if (i >= 52 && i <= 64 && i % 4 == 0)
           {
               isValid = true;
           }
           // 3. Extended Band (UNII-2e)
           // Standard Behavior: 100 - 144, Step 4
           else if (i >= 100 && i <= 144 && i % 4 == 0)
           {
               isValid = true;
           }
           // 4. Upper Band (UNII-3)
           // Standard Behavior: 149 - 165, Step 4 (Starts at 149)
           else if (i >= 149 && i <= 165 && i % 4 == 1)
           {
               isValid = true;
           }

           if (isValid)
           {
               var ch = new Channel(BandType.Band5GHz, i);
               list.Add(ch);
               CalculateGridPosition(ch, list.Count);
           }
       }
       return list;
   }
 */