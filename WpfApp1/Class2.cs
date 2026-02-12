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
*/