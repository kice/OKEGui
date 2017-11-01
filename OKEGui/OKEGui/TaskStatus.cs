using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace OKEGui
{
    //继承自INotifyPropertyChanged接口来实现数据的实时显示
    public class TaskStatus : INotifyPropertyChanged
    {
        /// <summary>
        /// 任务是否启用
        /// </summary>
        private bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set {
                isEnabled = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsEnabled"));
            }
        }

        /// <summary>
        /// 任务名称
        /// </summary>
        private string taskName;

        public string TaskName
        {
            get { return taskName; }
            set {
                taskName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TaskName"));
            }
        }

        /// <summary>
        /// 输入文件
        /// </summary>
        private string inputFile;

        public string InputFile
        {
            get { return inputFile; }
            set {
                inputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputFile"));
            }
        }

        /// <summary>
        /// 输出文件
        /// </summary>
        private string outputFile;

        public string OutputFile
        {
            get { return outputFile; }
            set {
                outputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
            }
        }

        /// <summary>
        /// 任务执行状态
        /// </summary>
        private string status;

        public string Status
        {
            get { return status; }
            set {
                status = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Status"));
            }
        }

        /// <summary>
        /// 当前进度（子任务进度）
        /// </summary>
        private double progress;

        public double Progress
        {
            get { return progress; }
            set {
                progress = value;
                // Indetermate
                if (progress < 0)
                {
                    ProgressStr = "";
                    IsUnKnowProgress = true;
                }
                else
                {
                    ProgressStr = progress.ToString("0.00") + "%";
                    IsUnKnowProgress = false;
                }

                OnPropertyChanged(new PropertyChangedEventArgs("Progress"));
            }
        }

        private string progressStr;

        public string ProgressStr
        {
            get { return progressStr; }
            set {
                progressStr = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ProgressStr"));
            }
        }

        /// <summary>
        /// 任务进度状态（进度未知）
        /// </summary>
        private bool isUnKnowProgress;

        public bool IsUnKnowProgress
        {
            get { return isUnKnowProgress; }
            set {
                isUnKnowProgress = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsUnKnowProgress"));
            }
        }

        /// <summary>
        /// 任务速度
        /// </summary>
        private string speed;

        public string Speed
        {
            get { return speed; }
            set {
                speed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Speed"));
            }
        }

        /// <summary>
        /// 码率
        /// </summary>
        private string bitrate;

        public string BitRate
        {
            get { return bitrate; }
            set {
                bitrate = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BitRate"));
            }
        }

        /// <summary>
        /// 剩余时间
        /// </summary>
        private TimeSpan timeRemain;

        public TimeSpan TimeRemain
        {
            get { return timeRemain; }
            set {
                timeRemain = value;
                TimeRemainStr = value.ToString(@"hh\:mm\:ss");
                if (value.TotalHours > 24.0)
                {
                    TimeRemainStr = "大于一天";
                }
            }
        }

        private string timeRemainStr;

        public string TimeRemainStr
        {
            get { return timeRemainStr; }
            set {
                timeRemainStr = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TimeRemainStr"));
            }
        }

        public bool WasAborted { get; set; }

        public bool HasError { get; set; }

        public bool IsComplete { get; set; }

        public TimeSpan TimeElapsed { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }
}
