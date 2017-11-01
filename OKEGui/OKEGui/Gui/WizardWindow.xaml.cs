using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace OKEGui
{
    /// <summary>
    /// WizardWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WizardWindow : Window
    {
        private class NewTask : INotifyPropertyChanged
        {
            private string projectFile;

            public string ProjectFile
            {
                get { return projectFile; }

                set {
                    projectFile = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectFile"));
                }
            }

            private int configVersion;

            public int ConfigVersion
            {
                get { return configVersion; }
                set {
                    configVersion = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ConfigVersion"));
                }
            }

            private string projectPreview;

            public string ProjectPreview
            {
                get { return projectPreview; }

                set {
                    projectPreview = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("ProjectPreview"));
                }
            }

            private string taskNamePrefix;

            public string TaskNamePrefix
            {
                get { return taskNamePrefix; }

                set {
                    taskNamePrefix = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("TaskNamePrefix"));
                }
            }

            private string inputScript;

            public string InputScript
            {
                get { return inputScript; }

                set {
                    inputScript = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("InputScript"));
                }
            }

            private string vsscript;

            public string VSScript
            {
                get { return vsscript; }

                set {
                    vsscript = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("VSScript"));
                }
            }

            private ObservableCollection<string> inputFile;

            public ObservableCollection<string> InputFile
            {
                get {
                    if (inputFile == null)
                    {
                        inputFile = new ObservableCollection<string>();
                    }

                    return inputFile;
                }
            }

            // 最终成品 c:\xxx\123.mkv
            private string outputFile;

            public string OutputFile
            {
                get { return outputFile; }
                set {
                    outputFile = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
                }
            }

            // mp4, mkv, null
            private string containerFormat;

            public string ContainerFormat
            {
                get { return containerFormat; }
                set {
                    containerFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ContainerFormat"));
                }
            }

            // HEVC, AVC
            private string videoFormat;

            public string VideoFormat
            {
                get { return videoFormat; }
                set {
                    videoFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("VideoFormat"));
                }
            }

            public uint fpsNum;
            public uint fpsDen;

            // 23.976, 29.970,...
            private double fps;

            public double Fps
            {
                get { return fps; }
                set {
                    fps = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("FPS"));
                }
            }

            // FLAC, AAC(m4a), "AC3", "ALAC"
            private string audioFormat;

            public string AudioFormat
            {
                get { return audioFormat; }
                set {
                    audioFormat = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("AudioFormat"));
                }
            }

            // 音频码率
            private int audioBitrate;

            public int AudioBitrate
            {
                get { return audioBitrate; }
                set {
                    audioBitrate = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("audioBitrate"));
                }
            }

            private ObservableCollection<AudioInfo> audioTracks;

            public ObservableCollection<AudioInfo> AudioTracks
            {
                get {
                    if (audioTracks == null)
                    {
                        audioTracks = new ObservableCollection<AudioInfo>();
                    }

                    return audioTracks;
                }
            }

            private string encoderPath;

            public string EncoderPath
            {
                get { return encoderPath; }
                set {
                    encoderPath = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderPath"));
                }
            }

            private string encoderParam;

            public string EncoderParam
            {
                get { return encoderParam; }
                set {
                    encoderParam = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderParam"));
                }
            }

            private string encoderType;

            public string EncoderType
            {
                get { return encoderType; }
                set {
                    encoderType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderType"));
                }
            }

            private string encoderInfo;

            public string EncoderInfo
            {
                get { return encoderInfo; }
                set {
                    encoderInfo = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EncoderInfo"));
                }
            }

            private bool includeSub;

            public bool IncludeSub
            {
                get { return includeSub; }
                set {
                    includeSub = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("IncludeSub"));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        private NewTask wizardInfo = new NewTask();
        private TaskManager tm;

        public WizardWindow(ref TaskManager t)
        {
            InitializeComponent();
            taskWizard.BackButtonContent = "上一步";
            taskWizard.CancelButtonContent = "取消";
            taskWizard.FinishButtonContent = "完成";
            taskWizard.HelpButtonContent = "帮助";
            taskWizard.HelpButtonVisibility = Visibility.Hidden;
            taskWizard.NextButtonContent = "下一步";
            this.DataContext = wizardInfo;

            tm = t;
        }

        public class JsonProfile
        {
            public int Version { get; set; }
            public string ProjectName { get; set; }
            public string EncoderType { get; set; }
            public string Encoder { get; set; }
            public string EncoderParam { get; set; }
            public string ContainerFormat { get; set; }
            public string VideoFormat { get; set; }
            public double Fps { get; set; }
            public uint FpsNum { get; set; }
            public uint FpsDen { get; set; }
            public List<AudioInfo> AudioTracks { get; set; }
            public string InputScript { get; set; }
            public bool IncludeSub { get; set; }
        }

        private bool LoadJsonProfile(string profile)
        {
            // TODO: 测试
            // TODO: FLAC -> lossless(auto)
            string profileStr = File.ReadAllText(profile);
            JsonProfile okeProj;
            try
            {
                okeProj = JsonConvert.DeserializeObject<JsonProfile>(profileStr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString(), "项目配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            DirectoryInfo projDir = new DirectoryInfo(wizardInfo.ProjectFile).Parent;

            // 检查参数
            if (okeProj.Version != 2)
            {
                System.Windows.MessageBox.Show("当前版本OKE不支持该版本的配置文件", "配置文件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            wizardInfo.TaskNamePrefix = okeProj.ProjectName;

            if (okeProj.EncoderType.ToLower() != "x265")
            {
                System.Windows.MessageBox.Show("目前只能支持x265编码", "配置文件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            wizardInfo.EncoderType = okeProj.EncoderType.ToLower();

            // 获取编码器全路径
            FileInfo encoder = new FileInfo(projDir.FullName + "\\" + okeProj.Encoder);
            if (encoder.Exists)
            {
                wizardInfo.EncoderPath = encoder.FullName;
                wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);
            }
            else
            {
                System.Windows.MessageBox.Show("编码器好像不在json指定的地方（文件名错误？还有记得放在json文件同目录下）", "找不到编码器啊", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            wizardInfo.EncoderParam = okeProj.EncoderParam;
            wizardInfo.IncludeSub = okeProj.IncludeSub;

            Dictionary<string, ComboBoxItem> comboItems = new Dictionary<string, ComboBoxItem>() {
                { "MKV",    MKVContainer},
                { "MP4",    MP4Container },
                { "HEVC",   HEVCVideo},
                { "AVC",    AVCVideo },
                { "FLAC",   FLACAudio },
                { "AAC",    AACAudio },
                { "AC3",    AC3Audio },
            };

            // 设置封装格式
            wizardInfo.ContainerFormat = okeProj.ContainerFormat.ToUpper();
            if (wizardInfo.ContainerFormat != "MKV" && wizardInfo.ContainerFormat != "MP4" &&
                wizardInfo.ContainerFormat != "NULL" && wizardInfo.ContainerFormat != "RAW")
            {
                System.Windows.MessageBox.Show("MKV/MP4，只能这两种", "封装格式指定的有问题", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            comboItems[wizardInfo.ContainerFormat].IsSelected = true;

            // 设置视频编码
            if (okeProj.VideoFormat.ToUpper() != "HEVC")
            {
                System.Windows.MessageBox.Show("现在只能支持HEVC编码", "编码格式不对", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            wizardInfo.VideoFormat = okeProj.VideoFormat.ToUpper();
            comboItems[wizardInfo.VideoFormat].IsSelected = true;

            // 设置视频帧率
            wizardInfo.Fps = okeProj.Fps;
            if (okeProj.Fps <= 0)
            {
                if (okeProj.Fps <= 0)
                {
                    if (okeProj.FpsNum <= 0 || okeProj.FpsDen <= 0)
                    {
                        System.Windows.MessageBox.Show("现在json文件中需要指定帧率，哪怕 Fps : 23.976", "帧率没有指定诶", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    wizardInfo.fpsNum = okeProj.FpsNum;
                    wizardInfo.fpsDen = okeProj.FpsDen;
                    wizardInfo.Fps = okeProj.FpsNum / okeProj.FpsDen;
                }

                return false;
            }

            if (okeProj.AudioTracks.Count > 0)
            {
                // 主音轨
                wizardInfo.AudioFormat = okeProj.AudioTracks[0].OutputCodec.ToUpper();
                wizardInfo.AudioBitrate = okeProj.AudioTracks[0].Bitrate;

                // 添加音频参数到任务里面
                foreach (var track in okeProj.AudioTracks)
                {
                    AudioJob audioJob = new AudioJob(track.OutputCodec);

                    wizardInfo.AudioTracks.Add(track);
                }
            }

            if (wizardInfo.AudioFormat != "FLAC" && wizardInfo.AudioFormat != "AAC" &&
                wizardInfo.AudioFormat != "AC3")
            {
                System.Windows.MessageBox.Show("音轨只能是FLAC/AAC/AC3", "音轨格式不支持", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            comboItems[wizardInfo.AudioFormat].IsSelected = true;

            var scriptFile = new FileInfo(projDir.FullName + "\\" + okeProj.InputScript);

            if (scriptFile.Exists)
            {
                wizardInfo.InputScript = scriptFile.FullName;
                wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);
            }

            // 预览
            wizardInfo.ProjectPreview = "项目名字: " + wizardInfo.TaskNamePrefix;
            wizardInfo.ProjectPreview += "\n\n编码器类型: " + wizardInfo.EncoderType;
            wizardInfo.ProjectPreview += "\n编码器路径: \n" + wizardInfo.EncoderPath;
            wizardInfo.ProjectPreview += "\n编码参数: \n" + wizardInfo.EncoderParam.Substring(0, Math.Min(30, wizardInfo.EncoderParam.Length - 1)) + "......";
            wizardInfo.ProjectPreview += "\n\n封装格式: " + wizardInfo.ContainerFormat;
            wizardInfo.ProjectPreview += "\n视频编码: " + wizardInfo.VideoFormat;
            wizardInfo.ProjectPreview += "\n视频帧率: " + String.Format("{0:0.000} fps", wizardInfo.Fps);
            wizardInfo.ProjectPreview += "\n音频编码(主音轨): " + wizardInfo.AudioFormat;

            return true;
        }

        private void OpenProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OKEGui 项目文件 (*.okeproj, *.json)|*.okeproj;*.json";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            wizardInfo.ProjectFile = ofd.FileName;
            if (new FileInfo(wizardInfo.ProjectFile).Extension.ToLower() == ".json")
            {
                if (!LoadJsonProfile(wizardInfo.ProjectFile))
                {
                    // 配置文件无效
                    taskWizard.CanSelectNextPage = false;
                }
                else
                {
                    taskWizard.CanSelectNextPage = true;
                }
                return;
            }

            throw new Exception("错误的配置文件");
        }

        private void OpenScriptBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "VapourSynth脚本 (*.vpy)|*.vpy";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            wizardInfo.InputScript = ofd.FileName;
            wizardInfo.VSScript = File.ReadAllText(wizardInfo.InputScript);

            SelectVSScript.CanSelectNextPage = true;
        }

        private string GetEncoderInfo(string EncoderPath)
        {
            Process proc = new Process();
            ProcessStartInfo pstart = new ProcessStartInfo();
            pstart.FileName = EncoderPath;
            pstart.Arguments = "-V";
            pstart.RedirectStandardOutput = true;
            pstart.RedirectStandardError = true;
            pstart.WindowStyle = ProcessWindowStyle.Hidden;
            pstart.CreateNoWindow = true;
            pstart.UseShellExecute = false;
            proc.StartInfo = pstart;
            proc.EnableRaisingEvents = true;
            try
            {
                bool started = proc.Start();
            }
            catch (Exception e)
            {
                throw e;
            }

            proc.WaitForExit();

            StreamReader sr = null;
            string line = "";
            try
            {
                sr = proc.StandardError;
                line = sr.ReadToEnd();
            }
            catch (Exception)
            {
                throw;
            }

            return line;
        }

        private void SelectEncoder_Loaded(object sender, RoutedEventArgs e)
        {
            if (wizardInfo.EncoderPath != "")
            {
                SelectEncoder.CanSelectNextPage = true;
            }
        }

        private void OpenEncoderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "视频编码器 (*.exe)|*.exe";
            var result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            wizardInfo.EncoderPath = ofd.FileName;
            wizardInfo.EncoderInfo = this.GetEncoderInfo(wizardInfo.EncoderPath);

            SelectEncoder.CanSelectNextPage = true;
        }

        private void WalkDirectoryTree(System.IO.DirectoryInfo root, Action<FileInfo> func, bool IsSearchSubDir = true)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // log.Add(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                // Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    func(fi);
                }

                if (IsSearchSubDir)
                {
                    // 搜索子目录
                    subDirs = root.GetDirectories();
                    foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                    {
                        WalkDirectoryTree(dirInfo, func, IsSearchSubDir);
                    }
                }
            }
        }

        private void OpenInputFile_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "视频文件 (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }

                if (!wizardInfo.VSScript.Contains("#OKE:INPUTFILE"))
                {
                    if (wizardInfo.InputFile.Count > 1 || ofd.FileNames.Length > 1)
                    {
                        System.Windows.MessageBox.Show("添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。点击上一步可以重新选择VapourSynth脚本。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                foreach (var filename in ofd.FileNames)
                {
                    if (!wizardInfo.InputFile.Contains(filename))
                    {
                        wizardInfo.InputFile.Add(filename);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(filename + "被重复选择，已取消添加。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                if (wizardInfo.InputFile.Count > 0)
                {
                    SelectInputFile.CanSelectNextPage = true;
                }
            }
        }

        private void OpenInputFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "请选择视频文件夹";
            // fbd.SelectedPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            fbd.SelectedPath = "C:\\";
            DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            string dir = fbd.SelectedPath.Trim();

            // 历遍改目录，添加全部支持的文件类型
            // 默认为m2ts, mp4, mkv
            MessageBoxResult isSearchSub = System.Windows.MessageBox.Show("是否搜索子目录？", "新建任务向导", MessageBoxButton.YesNo);
            WalkDirectoryTree(new DirectoryInfo(dir), (FileInfo fi) =>
            {
                if (fi.Extension.ToUpper() == ".M2TS" || fi.Extension.ToUpper() == ".MP4" ||
                    fi.Extension.ToUpper() == ".MKV")
                {
                    // TODO: 重复文件处理
                    if (!wizardInfo.InputFile.Contains(fi.FullName))
                    {
                        wizardInfo.InputFile.Add(fi.FullName);
                    }
                }
            }, isSearchSub == MessageBoxResult.Yes);

            if (wizardInfo.InputFile.Count > 1 && !wizardInfo.VSScript.Contains("#OKE:INPUTFILE"))
            {
                System.Windows.MessageBox.Show("添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。点击上一步可以重新选择VapourSynth脚本。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SelectInputFile.CanSelectNextPage = wizardInfo.InputFile.Count != 0;
        }

        private void SelectFormat_Leave(object sender, RoutedEventArgs e)
        {
            string container = ContainerFormat.Text;
            string video = VideoFormat.Text;
            string audio = AudioFormat.Text;

            if (container == "不封装")
            {
                container = "";
            }

            // 确保MP4封装的音轨只可能是AAC或者AC3
            if (container == "MP4")
            {
                bool hasFLAC = false;
                foreach (var audioTrack in wizardInfo.AudioTracks)
                    if (audioTrack.OutputCodec.ToUpper() == "FLAC" && !audioTrack.SkipMuxing)
                    {
                        hasFLAC = true;
                        audioTrack.OutputCodec = "AAC";
                        audioTrack.Bitrate = 256;
                    }
                if (hasFLAC)
                {
                    System.Windows.MessageBox.Show("格式选择错误！\nMP4不能封装FLAC格式。音频格式将改为AAC。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    audio = "AAC";
                }
            }

            wizardInfo.ContainerFormat = container;
            wizardInfo.VideoFormat = video;
            wizardInfo.AudioFormat = audio;
        }

        private void WizardFinish(object sender, RoutedEventArgs e)
        {
            // 检查输入脚本是否为oke模板
            var isTemplate = wizardInfo.VSScript.Contains("#OKE:INPUTFILE");

            // 使用正则解析模板, 多行忽略大小写
            Regex r = new Regex("#OKE:INPUTFILE([\\n\\r ]+\\w+[ ]*=[ ]*)([r]*[\"'].+[\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var inputTemplate = r.Split(wizardInfo.VSScript);
            if (inputTemplate.Length < 4 && wizardInfo.InputFile.Count() > 1)
            {
                System.Windows.MessageBox.Show("任务创建失败！添加多个输入文件请确保VapourSynth脚本使用OKE提供的模板。", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 处理DEBUG标签
            // TODO: 是否进行调试输出
            if (wizardInfo.VSScript.Contains("#OKE:DEBUG") && true)
            {
                Regex dr = new Regex("#OKE:DEBUG([\\n\\r ]+\\w+[ ]*=[ ]*)(\\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var debugTag = dr.Split(inputTemplate[3]);
                if (debugTag.Length < 4)
                {
                    // error
                    System.Windows.MessageBox.Show("Debug标签语法错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                inputTemplate[3] = debugTag[0] + debugTag[1] + "None" + debugTag[3];
            }

            // 新建任务
            // 1、新建脚本文件
            // 2、新建任务参数
            foreach (var inputFile in wizardInfo.InputFile)
            {
                MediaFile mf = new MediaFile();

                // 新建文件（inputname.m2ts-mm-dd-HH-MM.vpy）
                string vpy = inputTemplate[0] + inputTemplate[1] + "r'" + inputFile + "'" + inputTemplate[3];

                DateTime time = DateTime.Now;

                string fileName = inputFile + "-" + time.ToString("MMddHHmm") + ".vpy";
                File.WriteAllText(fileName, vpy);

                var finfo = new FileInfo(inputFile);
                TaskDetails task = new TaskDetails(finfo.Name);
                if (wizardInfo.TaskNamePrefix != "")
                {
                    task.TaskName = wizardInfo.TaskNamePrefix + "-" + task.TaskName;
                }

                task.ContainerFormat = wizardInfo.ContainerFormat;

                task.MediaInFile = new MediaFile(inputFile);

                // 新建视频处理工作
                if (wizardInfo.VideoFormat == "HEVC")
                {
                    VideoJob videoJob = new VideoJob(wizardInfo.VideoFormat);

                    videoJob.Input = new VSMediaFile(fileName);
                    videoJob.Output = task.MediaOutFile;
                    videoJob.EncoderPath = wizardInfo.EncoderPath;
                    videoJob.EncodeParam = wizardInfo.EncoderParam;
                    videoJob.Fps = wizardInfo.Fps;

                    task.JobQueue.Enqueue(videoJob);
                }

                task.VideoSettings = new VideoCodecSettings();
                task.AudioSettings = new AudioCodecSettings();

                task.IncludeSub = wizardInfo.IncludeSub;

                foreach (var audio in wizardInfo.AudioTracks)
                {
                    task.AudioTracks.Add(audio);
                }

                // 更新输出文件拓展名
                if (!task.UpdateOutputFileName())
                {
                    System.Windows.MessageBox.Show("格式错误！", "新建任务向导", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                task.IsExtAudioOnly = false;

                tm.AddTask(task);
            }
        }

        private void InputFile_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            object o = InputList.SelectedItem;
            if (o == null)
            {
                return;
            }

            String input = o as string;
            int id = wizardInfo.InputFile.IndexOf(input);
            if (id == -1)
            {
                // 没有找到
                return;
            }

            wizardInfo.InputFile.RemoveAt(id);
        }

        private void DeleteInput_Click(object sender, RoutedEventArgs e)
        {
            var list = InputList.SelectedItems;

            if (list.Count == 0)
            {
                return;
            }

            if (list.Count > 1)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("是否删除" + (list.Count.ToString()) + "个文件？", "新建任务向导", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            List<object> selectList = new List<object>();
            foreach (object item in list)
            {
                selectList.Add(item);
            }

            for (int i = 0; i < selectList.Count; i++)
            {
                foreach (object item in selectList)
                {
                    String selected = item as string;
                    int index = wizardInfo.InputFile.IndexOf(selected);
                    if (index != -1)
                    {
                        wizardInfo.InputFile.RemoveAt(index);
                    }
                }
            }

            SelectInputFile.CanSelectNextPage = wizardInfo.InputFile.Count != 0;
        }

        private void SelectVSScript_Loaded(object sender, RoutedEventArgs e)
        {
            SelectVSScript.CanSelectNextPage = wizardInfo.VSScript != "";
        }
    }
}
