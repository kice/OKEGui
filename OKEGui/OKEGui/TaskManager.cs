using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OKEGui
{
    // 线程安全Collection
    public class MTObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;
            if (CollectionChanged != null)
                foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
                {
                    DispatcherObject dispObj = nh.Target as DispatcherObject;
                    if (dispObj != null)
                    {
                        Dispatcher dispatcher = dispObj.Dispatcher;
                        if (dispatcher != null && !dispatcher.CheckAccess())
                        {
                            dispatcher.BeginInvoke(
                                (Action)(() => nh.Invoke(this,
                                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                                DispatcherPriority.DataBind);
                            continue;
                        }
                    }
                    nh.Invoke(this, e);
                }
        }
    }

    public class TaskManager
    {
        //List<update> updates = new List<update>();
        //List<T>不支持添加 删除数据时UI界面的响应,所以改用ObservableCollection<T>
        public MTObservableCollection<TaskDetails> taskStatus = new MTObservableCollection<TaskDetails>();

        private int newTaskCount = 1;
        private int tidCount = 0;

        public bool isCanStart = false;
        private object o = new object();

        public bool CheckTask(TaskDetails td)
        {
            if (!new FileInfo(td.MediaInFile.Path).Exists)
            {
                return false;
            }

            if (new FileInfo(td.MediaInFile.Path).Exists)
            {
                // 输出文件不存在
                throw new Exception("输出文件已存在");
                return false;
            }

            return true;
        }

        public int AddTask(TaskDetails detail)
        {
            TaskDetails td = detail;

            if (td.TaskName == "")
            {
                td.TaskName = "新建任务 - " + newTaskCount.ToString();
                newTaskCount = newTaskCount + 1;
            }

            if (!CheckTask(td))
            {
                return -1;
            }

            tidCount++;

            // 初始化任务参数
            td.Tid = tidCount.ToString();
            td.Status.IsEnabled = true;                            // 默认启用
            td.Status.Status = "等待中";
            td.Status.Progress = 0.0;
            td.Status.Speed = "0.0 fps";
            td.Status.TimeRemain = TimeSpan.FromDays(30);
            td.WorkerName = "";

            taskStatus.Add(td);

            return taskStatus.Count;
        }

        public bool DeleteTask(TaskDetails detail)
        {
            return this.DeleteTask(detail.Tid);
        }

        public bool DeleteTask(string tid)
        {
            if (Int32.Parse(tid) < 1)
            {
                return false;
            }

            try
            {
                foreach (var item in taskStatus)
                {
                    if (item.Tid == tid)
                    {
                        if (item.IsRunning)
                        {
                            return false;
                        }
                        taskStatus.Remove(item);
                        return true;
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            return false;
        }

        public bool UpdateTask(TaskDetails detail)
        {
            if (!CheckTask(detail))
            {
                return false;
            }

            if (Int32.Parse(detail.Tid) < 1)
            {
                return false;
            }

            taskStatus[Int32.Parse(detail.Tid) - 1] = detail;

            return true;
        }

        public TaskDetails GetNextTask()
        {
            if (!isCanStart)
            {
                return null;
            }

            lock (o)
            {
                // 找出下一个可用任务
                foreach (var task in taskStatus)
                {
                    if (task.Status.IsEnabled)
                    {
                        task.Status.IsEnabled = false;
                        task.IsRunning = true;
                        return task;
                    }
                }
            }

            return null;
        }
    }
}
