namespace OKEGui
{
    public delegate void JobProcessingStatusUpdateCallback(TaskStatus su);

    /// <summary>
    /// 任务处理。可执行单元
    /// </summary>
    public interface IJobProcessor
    {
        /// <summary>
        /// sets up encoding
        /// </summary
        /// <param name="job">the job to be processed</param>
        void Setup(Job job, TaskStatus su);

        /// <summary>
        /// starts the encoding process
        /// </summary>
        void Start();

        /// <summary>
        /// stops the encoding process
        /// </summary>
        void Stop();

        /// <summary>
        /// pauses the encoding process
        /// </summary>
        void Pause();

        /// <summary>
        /// resumes the encoding process
        /// </summary>
        void Resume();

        /// <summary>
        /// wait until job is finished
        /// </summary>
        void WaitForFinish();

        /// <summary>
        /// changes the priority of the encoding process/thread
        /// </summary>
        void ChangePriority(ProcessPriority priority);

        event JobProcessingStatusUpdateCallback StatusUpdate;
    }
}
