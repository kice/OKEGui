﻿using System;
using System.IO;

namespace OKEGui
{
    public delegate void EncoderOutputCallback(string line, int type);

    public abstract class CommandlineVideoEncoder : CommandlineJobProcessor
    {
        #region variables

        private ulong numberOfFrames;
        private ulong? currentFrameNumber;
        protected long fps_n = 0, fps_d = 0;
        protected bool usesSAR = false;
        protected double speed;
        protected double bitrate;
        protected string unit;

        protected VideoJob job;

        #endregion variables

        public CommandlineVideoEncoder() : base()
        {
        }

        #region helper methods

        /// <summary>
        /// tries to open the video source and gets the number of frames from it, or
        /// exits with an error
        /// </summary>
        /// <param name="videoSource">the AviSynth script</param>
        /// <param name="error">return parameter for all errors</param>
        /// <returns>true if the file could be opened, false if not</returns>
        protected void getInputProperties(VideoJob job)
        {
            //VapourSynthHelper vsHelper = new VapourSynthHelper();
            //vsHelper.LoadScriptFile(job.Input);
            VSPipeInfo vsHelper = new VSPipeInfo(job.Input);
            fps_n = vsHelper.FpsNum;
            fps_d = vsHelper.FpsDen;
            numberOfFrames = (ulong)vsHelper.TotalFreams;
            // su.ClipLength = TimeSpan.FromSeconds((double)numberOfFrames / fps);
        }

        /// <summary>
        /// compiles final bitrate statistics
        /// </summary>
        protected void compileFinalStats()
        {
            try {
                if (!string.IsNullOrEmpty(job.Output) && File.Exists(job.Output)) {
                    FileInfo fi = new FileInfo(job.Output);
                    long size = fi.Length; // size in bytes

                    ulong framecount = 0;
                    double framerate = 23.976;
                    // JobUtil.getInputProperties(out framecount, out framerate, job.Input);

                    double numberOfSeconds = (double)framecount / framerate;
                    long bitrate = (long)((double)(size * 8.0) / (numberOfSeconds * 1000.0));
                }
            } catch (Exception e) {
                // log.LogValue("Exception in compileFinalStats", e, ImageType.Warning);
            }
        }

        #endregion helper methods

        public override void setup(Job job, StatusUpdate su)
        {
        }

        protected bool setFrameNumber(string frameString)
        {
            int currentFrameNumber;
            if (int.TryParse(frameString, out currentFrameNumber)) {
                if (currentFrameNumber < 0) {
                    this.currentFrameNumber = 0;
                } else {
                    this.currentFrameNumber = (ulong)currentFrameNumber;
                }
                Update();
                return true;
            }
            return false;
        }

        protected bool setSpeed(string speed)
        {
            double fps;
            if (double.TryParse(speed, out fps)) {
                if (fps > 0) {
                    this.speed = fps;
                } else {
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
            if (double.TryParse(bitrate, out rate)) {
                if (rate > 0) {
                    this.bitrate = rate;
                } else {
                    this.bitrate = 0;
                }

                Update();
                return true;
            }

            return false;
        }

        protected void Update()
        {
            if (speed == 0) {
                job.config.TimeRemain = TimeSpan.FromDays(30);
            } else {
                job.config.TimeRemain = TimeSpan.FromSeconds((double)(numberOfFrames - currentFrameNumber) / speed);
            }
            job.config.Speed = speed.ToString() + " fps";
            job.config.ProgressValue = (double)currentFrameNumber / (double)numberOfFrames * 100;

            if (bitrate == 0) {
                job.config.BitRate = "未知";
            } else {
                job.config.BitRate = bitrate.ToString() + unit;
            }

            // su.NbFramesDone = currentFrameNumber;
        }

        private static String HumanReadableFilesize(double size, int digit)
        {
            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            while (size >= mod) {
                size /= mod;
                i++;
            }

            return Math.Round(size * Math.Pow(10, digit)) / Math.Pow(10, digit) + units[i];
        }

        protected void encodeFinish()
        {
            job.config.TimeRemain = TimeSpan.Zero;
            job.config.ProgressValue = 100;
            job.config.Status = "压制完成";

            // TODO: 计算最终码率
            // 这里显示文件最终大小
            FileInfo vinfo = new FileInfo(job.config.InputFile + ".hevc");
            job.config.BitRate = HumanReadableFilesize(vinfo.Length, 2);

            base.SetFinish();
        }
    }
}
