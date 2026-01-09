using System;
using System.Threading;
using System.Threading.Tasks;

public class ClassA
{
    // The main flag indicating work is being done
    private bool _isBusy = false;

    // The "Waiting Room" for the single pending item. 
    // If this is not null, it means someone is waiting in line.
    private TaskCompletionSource<bool> _pendingJobTcs;

    // Lock object to protect our state (_isBusy and _pendingJobTcs)
    private readonly object _stateLock = new object();

    public bool AwaitingEvent => _isBusy;

    public async Task Run()
    {
        TaskCompletionSource<bool> myWaitTicket = null;
        bool amIRunningImmediately = false;

        // --- STEP 1: Determine if we run, queue, or reject ---
        lock (_stateLock)
        {
            if (!_isBusy)
            {
                // Scenario A: Free to run immediately
                _isBusy = true;
                amIRunningImmediately = true;
            }
            else
            {
                if (_pendingJobTcs != null)
                {
                    // Scenario B: Busy AND Queue is full. 
                    // Reject this call completely (Rule #1).
                    return;
                }

                // Scenario C: Busy, but Queue is empty. Queue this call.
                _pendingJobTcs = new TaskCompletionSource<bool>();
                myWaitTicket = _pendingJobTcs;
            }
        }

        // --- STEP 2: Execute or Wait ---
        if (amIRunningImmediately)
        {
            await DoWorkAndProcessQueue();
        }
        else if (myWaitTicket != null)
        {
            // We are queued. Wait for the ticket to be called OR 3s timeout.
            // Task.WhenAny returns the first task to finish (the signal or the timer)
            var completedTask = await Task.WhenAny(myWaitTicket.Task, Task.Delay(3000));

            if (completedTask == myWaitTicket.Task)
            {
                // We were signaled! Now we run.
                await DoWorkAndProcessQueue();
            }
            else
            {
                // Timeout happened (Rule #2).
                HandleTimeout();
            }
        }
    }

    // The actual work logic, wrapped to ensure the queue is processed afterwards
    private async Task DoWorkAndProcessQueue()
    {
        try
        {
            // Do the actual remote work
            await someService.RemoteWorkAsync();
        }
        catch (Exception ex)
        {
            // Log error
        }
        finally
        {
            // When work finishes, check if anyone is waiting
            AdvanceQueue();
        }
    }

    private void AdvanceQueue()
    {
        lock (_stateLock)
        {
            if (_pendingJobTcs != null)
            {
                // Someone is waiting!
                var nextJob = _pendingJobTcs;
                _pendingJobTcs = null; // Clear the queue slot

                // Signal the waiting thread to wake up.
                // Note: _isBusy remains true because the next guy takes over immediately.
                nextJob.TrySetResult(true);
            }
            else
            {
                // No one waiting, we are done.
                _isBusy = false;
            }
        }
    }

    private void HandleTimeout()
    {
        lock (_stateLock)
        {
            // 1. Remove ourselves from the queue
            if (_pendingJobTcs != null && !_pendingJobTcs.Task.IsCompleted)
            {
                _pendingJobTcs = null;
            }

            // 2. Requirement: "In this case the _isAwaitingEvent should also be reset."
            // This is a safety valve. If the running thread is stuck for >3s,
            // we force the flag to false so future calls can pass.
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
*/