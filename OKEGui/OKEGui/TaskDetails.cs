using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    public class TaskDetails
    {
        // 工作队列
        public Queue<Job> JobQueue = new Queue<Job>();

        public TaskStatus Status;

        #region TaskInfo

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 是否在运行
        /// </summary>

        public bool IsRunning { get; set; }

        /// <summary>
        /// 任务ID
        /// </summary>
        public string Tid { get; set; }

        /// <summary>
        /// 正在执行的工作单元名称
        /// </summary>

        public string WorkerName { get; set; }

        /// <summary>
        /// 输入文件
        /// </summary>
        public MediaFile MediaInFile = new MediaFile();

        /// <summary>
        /// 输出文件
        /// </summary>
        public MediaFile MediaOutFile = new MediaFile();

        /// <summary>
        /// 封装容器格式
        /// </summary>
        /// <remarks>e.g. mp4, mkv, none(不封装，纯编码任务)</remarks>

        public string ContainerFormat { get; set; }

        /// <summary>
        /// 视频编码参数
        /// </summary>
        public VideoCodecSettings VideoSettings { get; set; }

        /// <summary>
        /// 音频编码参数
        /// </summary>
        public AudioCodecSettings AudioSettings { get; set; }

        /// <summary>
        /// 是否只抽取(并转码)音轨
        /// </summary>
        public bool IsExtAudioOnly { get; set; }

        /// <summary>
        /// 是否包含字幕
        /// </summary>
        public bool IncludeSub { get; set; }

        #endregion TaskInfo

        public TaskDetails(string taskName)
        {
            this.TaskName = Status.TaskName = taskName;
        }

        // 自动生成输出文件名
        //public bool UpdateOutputFileName()
        //{
        //    if (this.VideoFormat == "" || this.InputFile == "")
        //    {
        //        return false;
        //    }

        //    var finfo = new System.IO.FileInfo(this.InputFile);
        //    this.OutputFile = finfo.Name + "." + this.VideoFormat.ToLower();
        //    if (this.ContainerFormat != "")
        //    {
        //        this.OutputFile = finfo.Name + "." + this.ContainerFormat.ToLower();
        //    }

        //    return true;
        //}
    }
}
