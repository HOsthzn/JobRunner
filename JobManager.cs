/// <summary>
///     Used to Manage Jobs in the service
/// </summary>
public class JobManager
{
    /// <summary>
    ///     Execute all Jobs.
    /// </summary>
    public void ExecuteAllJobs( EventLog threadExecutionLog )
    {
        try
        {
            // ReSharper disable once CommentTypo
            // get all Ijob implementations of this assembly.
            IEnumerable< Type > jobs = GetAllTypesImplementingInterface( typeof( IJob ) );
            // execute each job
            if ( jobs == null ) return;
            foreach ( Type job in jobs )
            {
                //skip the base Job instance to prevent Unnecessary Event logging
                if ( job.IsInterface ) continue;
                // only instantiate the job if it's implementation is "real"
                if ( IsRealClass( job ) )
                    try
                    {
                        IJob instanceJob = ( IJob ) Activator.CreateInstance( job );
                        if ( instanceJob.IsRepeatable() )
                        {
                            Timer timer = new Timer { Interval = instanceJob.GetRepetitionIntervalTime() };
                            timer.Elapsed += instanceJob.OnTimer;

                            Task.Run( () =>
                                      {
                                          TaskFactory factory = instanceJob.IsLongRunning()
                                                                    ? new TaskFactory( TaskCreationOptions.AttachedToParent
                                                                                     , TaskContinuationOptions.LongRunning )
                                                                    : new TaskFactory( TaskCreationOptions.AttachedToParent
                                                                                     , TaskContinuationOptions.None );
                                          factory.StartNew( () => instanceJob.ExecuteJob( threadExecutionLog ) );
                                      } );
                            timer.Start();
                        }
                        else
                        {
                            Task.Run( () =>
                                      {
                                          TaskFactory factory = instanceJob.IsLongRunning()
                                                                    ? new TaskFactory( TaskCreationOptions.AttachedToParent
                                                                                     , TaskContinuationOptions.LongRunning )
                                                                    : new TaskFactory( TaskCreationOptions.AttachedToParent
                                                                                     , TaskContinuationOptions.None );
                                          factory.StartNew( () => instanceJob.ExecuteJob( threadExecutionLog ) );
                                      } );
                        }
                    }
                    catch ( Exception ex )
                    {
                        threadExecutionLog
                           .WriteEntry( $"Time: {DateTime.Now} \r\n Source: {ex.Source} \r\n Type: {ex.GetType()} \r\n TargetSite: {ex.TargetSite} \r\n Message: {ex.Message} \r\n StackTrace: {ex.StackTrace} \r\n Data: {ex.Data} \r\n InnerException: {ex.InnerException}"
                                      , EventLogEntryType.Error );
                    }
                else
                    threadExecutionLog
                       .WriteEntry( $"The Job \"{job.FullName}\" cannot be instantiated. \r\n Abstract: {job.IsAbstract}\r\n GenericTypeDefinition: {job.IsGenericTypeDefinition}\r\n Interface : {job.IsInterface}", EventLogEntryType.Error );
            }
        }
        catch ( Exception ex )
        {
            threadExecutionLog
               .WriteEntry( $"Time: {DateTime.Now} \r\n Source: {ex.Source} \r\n Type: {ex.GetType()} \r\n TargetSite: {ex.TargetSite} \r\n Message: {ex.Message} \r\n StackTrace: {ex.StackTrace} \r\n Data: {ex.Data} \r\n InnerException: {ex.InnerException}"
                          , EventLogEntryType.Error );
        }
    }

    /// <summary>
    ///     Returns all types in the current AppDomain implementing the interface or inheriting the type.
    /// </summary>
    private static IEnumerable< Type > GetAllTypesImplementingInterface( Type desiredType )
    {
        return AppDomain
              .CurrentDomain
              .GetAssemblies()
              .SelectMany( assembly => assembly.GetTypes() )
              .Where( desiredType.IsAssignableFrom );
    }

    /// <summary>
    ///     Determine whether the object is real - non-abstract, non-generic-needed, non-interface class.
    /// </summary>
    /// <param name="testType">Type to be verified.</param>
    /// <returns>True in case the class is real, false otherwise.</returns>
    private static bool IsRealClass( Type testType )
    {
        return testType.IsAbstract              == false
            && testType.IsGenericTypeDefinition == false
            && testType.IsInterface             == false;
    }
}

/// <summary>
///     Job Instance
/// </summary>
public interface IJob
{
    /// <summary>
    ///     Event Log Instance tu use for error logging
    /// </summary>
    EventLog ThreadExecutionLog { get; set; }

    /// <summary>
    ///     Execute the Job itself, one ore repeatedly, depending on
    ///     the job implementation.
    /// </summary>
    void ExecuteJob( EventLog threadExecutionLog );

    /// <summary>
    ///     for the on timer elapsed event, if used run DoJob from hear
    /// </summary>
    void OnTimer( object sender, ElapsedEventArgs e );

    /// <summary>
    ///     If this method is overridden, on can get within the job
    ///     parameters set just before the job is started. In this
    ///     situation the application is running and the use may have
    ///     access to resources which he/she has not during the thread
    ///     execution. For instance, in a web application, the user has
    ///     no access to the application context, when the thread is running.
    ///     Note that this method must not be overridden. It is optional.
    /// </summary>
    /// <returns>Parameters to be used in the job.</returns>
    object GetParameters();

    /// <summary>
    ///     Get the Job´s Name. This name uniquely identifies the Job.
    /// </summary>
    /// <returns>Job´s name.</returns>
    string GetName();

    /// <summary>
    ///     The job to be executed.
    /// </summary>
    void DoJob( EventLog threadExecutionLog );

    /// <summary>
    ///     Determines whether a Job is to be repeated after a
    ///     certain amount of time.
    /// </summary>
    /// <returns>True in case the Job is to be repeated, false otherwise.</returns>
    bool IsRepeatable();

    /// <summary>
    ///     will the thread executing this Job have to be long running
    /// </summary>
    /// <returns></returns>
    bool IsLongRunning();

    /// <summary>
    ///     The amount of time, in milliseconds, which the Job has to wait until it is started
    ///     over. This method is only useful if IJob.IsRepeatable() is true
    /// </summary>
    /// <returns>Interval time between this job executions.</returns>
    int GetRepetitionIntervalTime();
}
