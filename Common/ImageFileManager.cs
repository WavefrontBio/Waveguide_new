using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Waveguide
{   

    class ImageFileManager
    {      
        private int m_projectID;
        private int m_plateID;
        private int m_experimentID;
        private List<int> m_indicatorIDList;

        private string m_basePath;

        // Constructor
        public ImageFileManager()
        {
            m_basePath = "";
            m_indicatorIDList = new List<int>();
        }

        public void SetBasePath(string imageRootPath, int projectID, int plateID, int experimentID, List<int> indicatorIDList)
        {
            m_experimentID = experimentID;
            m_plateID = plateID;
            m_projectID = projectID;

            m_indicatorIDList.Clear();

            m_basePath = imageRootPath + "\\" + projectID.ToString() + "\\" + plateID.ToString() + "\\" + experimentID.ToString() + "\\";

            try
            {
                foreach (int id in indicatorIDList)
                {
                    m_indicatorIDList.Add(id);
                    Directory.CreateDirectory(m_basePath + id.ToString());
                }
                
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to Create Directory: " + m_basePath + "   " + e.Message);
            }
        }


        public string WriteImageFile(ushort[] imageData, int expIndicatorID, int msecs)
        {
            string fileName = m_basePath + expIndicatorID.ToString() + "\\" + msecs.ToString("D8") + "_wgi.zip";

            Zip.Compress_File(imageData, fileName);

            return fileName;
        }


        public void ReadImageFile(out ushort[] imageData, string fileName)
        {
            imageData = Zip.Decompress_File(fileName);
        }


    }
}
