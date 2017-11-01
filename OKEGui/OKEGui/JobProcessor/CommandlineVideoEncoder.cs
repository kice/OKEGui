using System;
using System.Diagnostics;
using System.IO;

namespace OKEGui
{
    public delegate void EncoderOutputCallback(string line, int type);

    public abstract class CommandlineVideoEncoder : CommandlineJobProcessor
    {
        #region variables

        protected new VideoJob job;
        private ulong numberOfFrames;

        private ulong currentFrameNumber;
        private ulong lastFrameNumber;
        private uint lastUpdateTime;
        protected long fps_n = 0, fps_d = 0;

        protected bool usesSAR = false;
        protected double speed;
        protected double bitrate;
        protected string unit;

        #endregion variables

        public CommandlineVideoEncoder() : base()
        {
            // 设置计时器精度 1ms
            timeBeginPeriod(1);
        }

        ~CommandlineVideoEncoder()
        {
            timeEndPeriod(1);
        }

        #region helper methods

        [System.Runtime.InteropServices.DllImport("winmm")]
        private static extern uint timeGetTime();

        [System.Runtime.InteropServices.DllImport("winmm")]
        private static extern void timeBeginPeriod(int t);

        [System.Runtime.InteropServices.DllImport("winmm")]
        private static extern void timeEndPeriod(int t);

        /// <summary>
        /// tries to open the video source and gets the number of frames from it, or
        /// exits with an error
        /// </summary>
        /// <param name="videoSource">the AviSynth script</param>
        /// <param name="error">return parameter for all errors</param>
        /// <returns>true if the file could be opened, false if not</returns>
        protected void getInputProperties(VideoJob job)
        {
            if (job.Input.Type == MediaFileType.VSScritpFile)
            {
                VSPipeInfo vsHelper = new VSPipeInfo(job.Input.Path);
                fps_n = vsHelper.FpsNum;
                fps_d = vsHelper.FpsDen;
                numberOfFrames = (ulong)vsHelper.TotalFreams;
            }
            else if (job.Input.Type == MediaFileType.NormalFile)
            {
                // TODO: 使用FFMPEG读取源信息
            }

            if (fps_n != job.FpsNum || fps_d != job.FpsDen)
            {
                throw new Exception("输出FPS和指定FPS不一致");
            }
        }

        /// <summary>
        /// compiles final bitrate statistics
        /// </summary>
        protected void compileFinalStats()
        {
            try
            {
                if (!string.IsNullOrEmpty(job.Output.Path) && File.Exists(job.Output.Path))
                {
                    FileInfo fi = new FileInfo(job.Output.Path);
                    long size = fi.Length; // size in bytes

                    ulong framecount = 0;
                    double framerate = fps_n / fps_d;

                    double numberOfSeconds = (double)framecount / framerate;
                    bitrate = (long)(size * 8.0 / (numberOfSeconds * 1000.0));
                }
            }
            catch (Exception e)
            {
                // log.LogValue("Exception in compileFinalStats", e, ImageType.Warning);
            }
        }

        #endregion helper methods

        public override void Setup(Job job, TaskStatus su)
        {
            if (!(job is VideoJob))
            {
                throw new Exception("错误的任务类型");
            }

            this.job = job as VideoJob;
            this.su = su;
        }

        protected bool setFrameNumber(string frameString, bool isUpdateSpeed = false)
        {
            int currentFrame;
            if (int.TryParse(frameString, out currentFrame))
            {
                if (currentFrame < 0)
                {
                    currentFrameNumber = 0;
                    lastFrameNumber = 0;
                    return false;
                }
                else
                {
                    currentFrameNumber = (ulong)currentFrame;
                }

                double time = timeGetTime() - lastUpdateTime;

                if (isUpdateSpeed && time > 1000)
                {
                    speed = ((currentFrameNumber - lastFrameNumber) / time) * 1000.0;

                    lastFrameNumber = currentFrameNumber;
                    lastUpdateTime = timeGetTime();
                }

                Update();
                return true;
            }
            return false;
        }

        protected bool setSpeed(string speed)
        {
            double fps;
            if (double.TryParse(speed, out fps))
            {
                if (fps > 0)
                {
                    this.speed = fps;
                }
                else
                {
                    this.speed = 0;
                }

                Update();
                return true;
            }

            return false;
        }

        protected bool setBitrate(string bitrate, string unit)
        {
            double rate;
            this.unit = unit;
            if (double.TryParse(bitrate, out rate))
            {
                if (rate > 0)
                {
                    this.bitrate = rate;
                }
                else
                {
                    this.bitrate = 0;
                }

                Update();
                return true;
            }

            return false;
        }

        protected void Update()
        {
            if (speed == 0)
            {
                su.TimeRemain = TimeSpan.FromDays(30);
            }
            else
            {
                su.TimeRemain = TimeSpan.FromSeconds((double)(numberOfFrames - currentFrameNumber) / speed);
            }

            su.Speed = speed.ToString("0.00") + " fps";
            su.Progress = (double)currentFrameNumber / (double)numberOfFrames * 100;

            if (bitrate == 0)
            {
                su.BitRate = "未知";
            }
            else
            {
                su.BitRate = bitrate.ToString("0.00") + unit;
            }

            // su.NbFramesDone = currentFrameNumber;
        }

        public static String HumanReadableFilesize(double size, int digit)
        {
            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }

            return Math.Round(size * Math.Pow(10, digit)) / Math.Pow(10, digit) + units[i];
        }

        protected void encodeFinish()
        {
            su.TimeRemain = TimeSpan.Zero;
            su.Progress = 100;
            su.Status = "压制完成";

            // compileFinalStats();

            // 这里显示文件最终大小
            FileInfo vinfo = new FileInfo(su.OutputFile);
            su.BitRate = HumanReadableFilesize(vinfo.Length, 2);

            base.SetFinish();
        }
    }
}
