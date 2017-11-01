using System;
using System.Collections.Generic;

namespace OKEGui
{
    public enum ProcessPriority : int { IDLE = 0, BELOW_NORMAL, NORMAL, ABOVE_NORMAL, HIGH, PARALLEL };

    public enum JobStatus : int { WAITING = 0, PROCESSING, POSTPONED, ERROR, ABORTED, DONE, SKIP, ABORTING };

    // status of job, 0: waiting, 1: processing, 2: postponed, 3: error, 4: aborted, 5: done, 6: skip, 7: aborting

    /// <summary>
    /// 任务信息
    /// </summary>
    public abstract class Job
    {
        #region important details

        public MediaFile Input;
        public MediaFile Output;

        public List<string> FilesToDelete;

        #region JobStatus

        private JobStatus ts;

        public void SetUpdate(JobStatus taskStatus)
        {
            ts = taskStatus;
        }

        #endregion JobStatus

        #endregion important details

        #region init

        public Job() : this(null, null)
        {
        }

        public Job(MediaFile input, MediaFile output)
        {
            FilesToDelete = new List<string>();

            Input = input;
            Output = output;
            if (input == null || output == null)
            {
                Input = new MediaFile();
                Output = new MediaFile();
            }

            if (!string.IsNullOrEmpty(input.Path) && input.Path == output.Path)
                throw new Exception("Input and output files may not be the same");
        }

        #endregion init

        #region queue display details

        /// <summary>
        /// 使用的编码格式
        /// </summary>
        public virtual string CodecString
        {
            get { return ""; }
        }

        /// <summary>
        /// 任务类型（VideoEncode, AudioEncode, Demux, Mux, Other）
        /// </summary>
        public abstract string JobType
        {
            get;
        }

        #endregion queue display details
    }
}
