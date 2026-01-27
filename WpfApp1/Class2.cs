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
*/