# JobRunner
Create and run multiple jobs simultaneously

## example

First, let's create a job that implements the IJob interface. This job will simply write a message to the console every time it is executed.

```csharp
public class HelloWorldJob : IJob
{
    public EventLog ThreadExecutionLog { get; set; }
    public void ExecuteJob(EventLog threadExecutionLog)
    {
        DoJob(threadExecutionLog);
    }
    public void OnTimer(object sender, ElapsedEventArgs e)
    {
        ExecuteJob(ThreadExecutionLog);
    }
    public object GetParameters()
    {
        return null;
    }
    public string GetName()
    {
        return "HelloWorldJob";
    }
    public void DoJob(EventLog threadExecutionLog)
    {
        Console.WriteLine("Hello, world!");
    }
    public bool IsRepeatable()
    {
        return true;
    }
    public bool IsLongRunning()
    {
        return false;
    }
    public int GetRepetitionIntervalTime()
    {
        return 1000; // Repeat every 1 second
    }
}
```

Now, let's use the JobManager to execute this job.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        JobManager jobManager = new JobManager();
        EventLog eventLog = new EventLog();
        jobManager.ExecuteAllJobs(eventLog);
        // Keep the console open
        Console.ReadLine();
    }
}
```

In this example, the HelloWorldJob will be executed every second, and it will write "Hello, world!" to the console each time it is executed. The JobManager will automatically find and execute this job because it implements the IJob interface.
