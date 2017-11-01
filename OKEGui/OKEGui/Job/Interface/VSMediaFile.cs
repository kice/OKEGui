using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    //TODO: inputSource

    public class VSMediaFile : MediaFile
    {
        public VSMediaFile(string inputScript)
        {
            if (new FileInfo(inputScript).Extension != ".vpy")
            {
                throw new Exception("非法的VS脚本文件");
            }

            this.Path = inputScript;
            Type = MediaFileType.VSScritpFile;
        }
    }
}
