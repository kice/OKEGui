using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace OKEGui
{
    // TODO: 目前只考虑压制全部任务；以后可能会各步骤分开进行，或者进行其他任务
    // TODO: TaskManger 做成接口。各种不同类型任务分开管理。
    public enum WorkerType
    {
        Normal,
        Temporary,
    }

    internal struct WorkerArgs
    {
        public string Name;
        public WorkerType RunningType;
        public TaskManager taskManager;
        public BackgroundWorker bgWorker;
    }

    public class WorkerManager
    {
        public TaskManager tm;

        private List<string> workerList;

        private object o = new object();

        private ConcurrentDictionary<string, BackgroundWorker> bgworkerlist;
        private ConcurrentDictionary<string, WorkerType> workerType;
        private int tempCounter;
        private bool isRunning;

        public delegate void Callback();

        public Callback AfterFinish = null;

        public WorkerManager(TaskManager taskManager)
        {
            workerList = new List<string>();
            bgworkerlist = new ConcurrentDictionary<string, BackgroundWorker>();
            workerType = new ConcurrentDictionary<string, WorkerType>();
            tm = taskManager;
            isRunning = false;
            tempCounter = 0;
        }

        public bool Start()
        {
            lock (o)
            {
                if (workerList.Count == 0)
                {
                    return false;
                }

                isRunning = true;

                foreach (string worker in workerList)
                {
                    if (bgworkerlist.ContainsKey(worker))
                    {
                        BackgroundWorker bg;
                        bgworkerlist.TryRemove(worker, out bg);
                    }

                    CreateWorker(worker);
                    StartWorker(worker);
                }

                return true;
            }
        }

        public bool CreateWorker(string name)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(WorkerDoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(WorkerProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);

            return bgworkerlist.TryAdd(name, worker);
        }

        public bool StartWorker(string name)
        {
            if (!bgworkerlist.ContainsKey(name))
            {
                return false;
            }

            var worker = bgworkerlist[name];

            WorkerArgs args;
            args.Name = name;
            args.RunningType = workerType[name];
            args.taskManager = tm;
            args.bgWorker = worker;

            worker.RunWorkerAsync(args);
            return true;
        }

        public int GetWorkerCount()
        {
            lock (o)
            {
                return workerList.Count;
            }
        }

        public string AddTempWorker()
        {
            // 临时Worker只运行一次任务
            tempCounter++;
            string name = "Temp-" + tempCounter.ToString();

            lock (o)
            {
                workerList.Add(name);
                workerType.TryAdd(name, WorkerType.Temporary);
            }

            if (isRunning)
            {
                CreateWorker(name);
                StartWorker(name);
            }

            return name;
        }

        public bool AddWorker(string name)
        {
            lock (o)
            {
                if (workerList.Contains(name))
                {
                    return false;
                }

                workerList.Add(name);
                workerType.TryAdd(name, WorkerType.Normal);
            }

            if (isRunning)
            {
                CreateWorker(name);
                StartWorker(name);
            }

            return true;
        }

        public bool DeleteWorker(string name)
        {
            if (isRunning)
            {
                return false;
            }

            lock (o)
            {
                BackgroundWorker v;
                bgworkerlist.TryRemove(name, out v);

                WorkerType w;
                workerType.TryRemove(name, out w);

                return workerList.Remove(name);
            }
        }

        public void StopWorker(string name)
        {
            // TODO
            isRunning = false;

            if (bgworkerlist.ContainsKey(name))
            {
                if (bgworkerlist[name].IsBusy)
                {
                    bgworkerlist[name].CancelAsync();
                }
            }
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (WorkerArgs)e.Argument;

            while (isRunning)
            {
                TaskDetails task = args.taskManager.GetNextTask();
                TaskStatus su = task.Status;

                // 检查是否已经完成全部任务
                if (task == null)
                {
                    // 全部工作完成
                    lock (o)
                    {
                        BackgroundWorker v;
                        bgworkerlist.TryRemove(args.Name, out v);

                        if (bgworkerlist.Count == 0 && workerType.Count == 0)
                        {
                            if (AfterFinish != null)
                            {
                                AfterFinish();
                            }
                        }
                    }
                    return;
                }

                su.IsEnabled = false;
                task.WorkerName = args.Name;
                task.IsRunning = true;

                // 新建工作
                // 抽取音轨
                FileInfo eacInfo = new FileInfo(".\\tools\\eac3to\\eac3to.exe");
                if (!eacInfo.Exists)
                {
                    throw new Exception("Eac3to 不存在");
                }
                MediaFile extAudioTracks = new EACDemuxer(eacInfo.FullName, task.MediaInFile.Path).Extract(
                    (double progress, EACProgressType type) =>
                    {
                        switch (type)
                        {
                            case EACProgressType.Analyze:
                                su.Status = "轨道分析中";
                                su.Progress = progress;
                                break;

                            case EACProgressType.Process:
                                su.Status = "抽取音轨中";
                                su.Progress = progress;
                                break;

                            case EACProgressType.Completed:
                                su.Status = "音轨抽取完毕";
                                su.Progress = progress;
                                break;

                            default:
                                return;
                        }
                    });

                var srcAudioTracks = task.MediaInFile.AudioTracks;

                // 新建音频处理工作
                if (extAudioTracks.AudioTracks.Count != srcAudioTracks.Count)
                {
                    new System.Threading.Tasks.Task(() =>
                        System.Windows.MessageBox.Show($"当前的视频含有轨道数{extAudioTracks.AudioTracks.Count}，与json中指定的数量{srcAudioTracks.Count}不符合。该文件{task.MediaInFile.Path}将跳过处理")).Start();
                    return;
                }

                for (int id = 0; id < extAudioTracks.AudioTracks.Count; id++)
                {
                    if (srcAudioTracks[id].StreamInfo.SkipMuxing)
                    {
                        continue;
                    }

                    // 只处理flac文件
                    if (extAudioTracks.AudioTracks[id].File.GetExtension() != ".flac")
                    {
                        continue;
                    }
                    AudioJob audioJob = new AudioJob(srcAudioTracks[id].StreamInfo.OutputCodec);
                    audioJob.Input.Path = extAudioTracks.AudioTracks[id].File.GetFullPath();

                    task.JobQueue.Enqueue(audioJob);
                }

                while (task.JobQueue.Count != 0)
                {
                    Job job = task.JobQueue.Dequeue();

                    if (job is AudioJob)
                    {
                        AudioJob audioJob = job as AudioJob;
                        if (audioJob.CodecString == "FLAC" ||
                            audioJob.CodecString == "AUTO")
                        {
                            // 跳过当前轨道
                            audioJob.Output = audioJob.Input;
                        }
                        else if (audioJob.CodecString == "AAC")
                        {
                            su.Status = "音频转码中";
                            su.IsUnKnowProgress = true;
                            AudioJob aDecode = new AudioJob("WAV");
                            aDecode.Input = audioJob.Input;
                            aDecode.Output.Path = "-";
                            FLACDecoder flac = new FLACDecoder(".\\tools\\flac\\flac.exe", aDecode);

                            AudioJob aEncode = new AudioJob("AAC");
                            aEncode.Input.Path = "-";
                            aEncode.Output.Path = Path.ChangeExtension(audioJob.Input.Path, ".aac");
                            QAACEncoder qaac = new QAACEncoder(".\\tools\\qaac\\qaac.exe", aEncode, audioJob.Bitrate > 0 ? audioJob.Bitrate : Utils.Constants.QAACBitrate);

                            CMDPipeJobProcessor cmdpipe = CMDPipeJobProcessor.NewCMDPipeJobProcessor(flac, qaac);
                            cmdpipe.Start();
                            cmdpipe.WaitForFinish();

                            audioJob.Output = aEncode.Output;
                        }
                        else
                        {
                            // 未支持格式
                            audioJob.Output = audioJob.Input;
                        }

                        var audioFileInfo = new FileInfo(audioJob.Output.Path);
                        if (audioFileInfo.Length < 1024)
                        {
                            // 无效音轨
                            File.Move(audioJob.Output.Path, Path.ChangeExtension(audioJob.Output.Path, ".bak") + audioFileInfo.Extension);
                            continue;
                        }

                        task.MediaOutFile.AddTrack(AudioTrack.NewTrack(new OKEFile(job.Output.Path)));
                    }
                    else if (job is VideoJob)
                    {
                        if (job.CodecString == "HEVC")
                        {
                            try
                            {
                                su.Status = "获取信息中";
                                su.IsUnKnowProgress = true;
                                IJobProcessor processor = x265Encoder.init(job, (job as VideoJob).EncodeParam);

                                su.Status = "压制中";
                                su.Progress = 0.0;
                                processor.Start();
                                processor.WaitForFinish();
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "x265编码任务错误");
                            }
                        }

                        task.MediaOutFile.AddTrack(VideoTrack.NewTrack(new OKEFile(job.Output.Path), (job as VideoJob).Fps));
                    }
                    else
                    {
                        // 不支持的工作
                    }
                }

                // 添加章节文件
                FileInfo txtChapter = new FileInfo(Path.ChangeExtension(task.MediaInFile.Path, ".txt"));
                if (txtChapter.Exists)
                {
                    task.MediaOutFile.AddTrack(ChapterTrack.NewTrack(new OKEFile(txtChapter)));
                }

                // 封装
                if (task.ContainerFormat != "")
                {
                    su.Status = "封装中";
                    FileInfo mkvInfo = new FileInfo(".\\tools\\mkvtoolnix\\mkvmerge.exe");
                    if (!mkvInfo.Exists)
                    {
                        throw new Exception("mkvmerge不存在");
                    }

                    FileInfo lsmash = new FileInfo(".\\tools\\l-smash\\muxer.exe");
                    if (!lsmash.Exists)
                    {
                        throw new Exception("l-smash 封装工具不存在");
                    }

                    AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                    muxer.ProgressChanged += progress => su.Progress = progress;

                    muxer.StartMuxing(Path.GetDirectoryName(task.MediaInFile.Path) + "\\" + task.MediaOutFile.Path, task.MediaOutFile);
                }

                su.Status = "完成";
                su.Progress = 100;
            }
        }

        private void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}
