using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace CudaToolsNet
{
    public class CudaToolBox
    {
        // Import the methods exported by the unmanaged D3DSurfaceManager.
        [DllImport("CudaTools.dll")]
        static extern IntPtr SetFullGrayscaleImage(IntPtr grayImage, UInt16 imageWidth, UInt16 imageHeight);

        [DllImport("CudaTools.dll")]
        static extern IntPtr SetRoiGrayscaleImage(IntPtr roiImage, UInt16 imageWidth, UInt16 imageHeight, UInt16 roiWidth, UInt16 roiHeight, UInt16 roiX, UInt16 roiY);

        [DllImport("CudaTools.dll")]
        static extern IntPtr GetGrayscaleImagePtr();

        [DllImport("CudaTools.dll")]
        static extern IntPtr SetMaskImage(IntPtr maskImage, UInt16 maskWidth, UInt16 maskHeight, UInt16 maskRows, UInt16 maskCols);

        [DllImport("CudaTools.dll")]
        static extern IntPtr GetMaskImagePtr();

        [DllImport("CudaTools.dll")]
        static extern void SetColorMap(IntPtr redMap, IntPtr greenMap, IntPtr blueMap, UInt16 maxPixelValue);

        [DllImport("CudaTools.dll")]
        static extern IntPtr ConvertGrayscaleToColor(UInt16 scaleLower, UInt16 scaleUpper);

        [DllImport("CudaTools.dll")]
        static extern IntPtr GetColorImagePtr();

        [DllImport("CudaTools.dll")]
        static extern void ApplyMaskToImage();

        [DllImport("CudaTools.dll")]
        static extern IntPtr PipelineFullImage(IntPtr grayImage, UInt16 imageWidth, UInt16 imageHeight, bool applyMask);

        [DllImport("CudaTools.dll")]
        static extern IntPtr PipelineRoiImage(IntPtr roiImage, UInt16 imageWidth, UInt16 imageHeight, UInt16 roiWidth, UInt16 roiHeight, UInt16 roiX, UInt16 roiY, bool applyMask);

        [DllImport("CudaTools.dll")]
        static extern void DownloadColorImage(IntPtr colorImageDest);

        [DllImport("CudaTools.dll")]
        static extern void DownloadGrayscaleImage(IntPtr grayscaleImageDest);

        [DllImport("CudaTools.dll")]
        static extern void Init();

        [DllImport("CudaTools.dll")]
        static extern void CudaInit(IntPtr pDeviceEx);        

        [DllImport("CudaTools.dll")]
        static extern void Shutdown();

        [DllImport("CudaTools.dll")]
        static extern void CopyGPUImageToD3DSurface(int surfaceIndex, IntPtr pData);

        [DllImport("CudaTools.dll")]
        static extern bool RemoveD3DSurface(int surfaceIndex);

        [DllImport("CudaTools.dll")]
        static extern bool AddNewD3DSurface(int surfaceIndex, IntPtr pSurface, int width, int height);

        [DllImport("CudaTools.dll")]
        static extern void GetHistogram_512Buckets(IntPtr destHist, byte maxValueBitWidth);

        [DllImport("CudaTools.dll")]
        static extern void GetHistogramImage_512Buckets(IntPtr histImage, UInt16 width, UInt16 height, UInt32 maxBinCount);

        [DllImport("CudaTools.dll")]
        static extern void CalculateMaskApertureSums(IntPtr sums);

        [DllImport("CudaTools.dll")]
        static extern void PushContext();

        [DllImport("CudaTools.dll")]
        static extern void PopContext();


        public void InitCudaTools(IntPtr pDeviceEx)
        {
            Init();
            CudaInit(pDeviceEx);
        }

        public void ShutdownCudaTools()
        {
            Shutdown();
        }


        public IntPtr PostFullGrayscaleImage(UInt16[] grayImage, UInt16 width, UInt16 height)
        {
            // copy 16-bit grayscale image to GPU
            GCHandle pinnedArray = GCHandle.Alloc(grayImage, GCHandleType.Pinned);
            IntPtr grayImagePointer = pinnedArray.AddrOfPinnedObject();
       
            IntPtr returnPtr = IntPtr.Zero;
            returnPtr = SetFullGrayscaleImage(grayImagePointer,  width,  height);

            pinnedArray.Free();

            return returnPtr;  // returns pointer to GPU memory where the gray image resides
        }


        public IntPtr PostRoiGrayscaleImage(UInt16[] roiImage, UInt16 width, UInt16 height, UInt16 roiWidth, UInt16 roiHeight, UInt16 roiX, UInt16 roiY)
        {
            // copy 16-bit roi grayscale image to GPU
            GCHandle pinnedArray = GCHandle.Alloc(roiImage, GCHandleType.Pinned);
            IntPtr roiImagePointer = pinnedArray.AddrOfPinnedObject();

            IntPtr returnPtr = IntPtr.Zero;            
            returnPtr = SetRoiGrayscaleImage(roiImagePointer, width, height, roiWidth, roiHeight, roiX, roiY);

            pinnedArray.Free();

            return returnPtr;  // returns pointer to GPU memory where the full (not just the roi) gray image resides
        }


        public IntPtr Set_MaskImage(UInt16[] maskImage, UInt16 maskWidth, UInt16 maskHeight, UInt16 maskRows, UInt16 maskCols)
        {
            // copy 16-bit image mask to GPU
           
            IntPtr returnPtr = IntPtr.Zero;

            // Initialize unmanaged memory to hold the array.
            int size = Marshal.SizeOf(maskImage[0]) * maskImage.Length;
            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy((Int16[])((object)maskImage), 0, pnt, maskImage.Length);  // 

                // copy unmanaged array to GPU
                returnPtr = SetMaskImage(pnt, maskWidth, maskHeight, maskRows, maskCols);
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }

            return returnPtr;  // returns pointer to GPU memory where the mask image resides
        }


        public void ApplyMaskToGrayscaleImage()
        {
            ApplyMaskToImage();
        }


        public void Set_ColorMap(byte[] redMap, byte[] greenMap, byte[] blueMap, UInt16 maxPixelValue)
        {
            // This funciton sets the color maps used for each color onto the GPU

            // Initialize unmanaged memory to hold the arrays
            int sizeRed = Marshal.SizeOf(redMap[0]) * redMap.Length;
            IntPtr pntRed = Marshal.AllocHGlobal(sizeRed);

            int sizeGreen = Marshal.SizeOf(greenMap[0]) * greenMap.Length;
            IntPtr pntGreen = Marshal.AllocHGlobal(sizeGreen);

            int sizeBlue = Marshal.SizeOf(blueMap[0]) * blueMap.Length;
            IntPtr pntBlue = Marshal.AllocHGlobal(sizeBlue);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(redMap,    0, pntRed,      redMap.Length);
                Marshal.Copy(greenMap,  0, pntGreen,    greenMap.Length);
                Marshal.Copy(blueMap,   0, pntBlue,     blueMap.Length);

                // copy color maps from unmanaged memory to GPU memory
                SetColorMap(pntRed, pntGreen, pntBlue, maxPixelValue);
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pntRed);
                Marshal.FreeHGlobal(pntGreen);
                Marshal.FreeHGlobal(pntBlue);
            }
        }



        public IntPtr Convert_GrayscaleToColor(UInt16 scaleLower, UInt16 scaleUpper)
        {
            IntPtr ptr = IntPtr.Zero;
            ptr = ConvertGrayscaleToColor(scaleLower, scaleUpper);
            return ptr;
        }

        public void Download_ColorImage(out byte[] colorImage, UInt16 width, UInt16 height)
        {
            colorImage = new byte[width * height * 4];
            GCHandle pinnedArray = GCHandle.Alloc(colorImage, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();

            DownloadColorImage(ptr);

            pinnedArray.Free();
        }

        public void Download_GrayscaleImage(out UInt16[] grayscaleImage, UInt16 width, UInt16 height)
        {
            grayscaleImage = new UInt16[width * height];
            GCHandle pinnedArray = GCHandle.Alloc(grayscaleImage, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();

            DownloadGrayscaleImage(ptr);

            pinnedArray.Free();
        }

        public void GetHistogram_512(out UInt32[] histogram, byte maxValueBitWidth)
        {
            histogram = new UInt32[512];
            GCHandle pinnedArray = GCHandle.Alloc(histogram, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();            

            GetHistogram_512Buckets(ptr, maxValueBitWidth);

            pinnedArray.Free();
        }


        public void GetHistogramImage_512(out byte[] histImage, UInt16 width, UInt16 height, UInt32 maxBinCount)
        {
            histImage = new byte[width * height * 4];
            GCHandle pinnedArray = GCHandle.Alloc(histImage, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();   

            GetHistogramImage_512Buckets(ptr, width, height, maxBinCount);

            pinnedArray.Free();
        }

        public void GetMaskApertureSums(out UInt32[] sums, int maskApertureRows, int maskApertureCols)
        {
            sums = new UInt32[maskApertureRows * maskApertureCols];
            GCHandle pinnedArray = GCHandle.Alloc(sums, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();   

            CalculateMaskApertureSums(ptr);

            pinnedArray.Free();
        }


        public void Copy_GpuImageToD3DSurface(int surfaceIndex, IntPtr pData)
        {
            CopyGPUImageToD3DSurface(surfaceIndex, pData); 
        }


        public bool Remove_D3dSurface(int surfaceIndex)
        {
            return RemoveD3DSurface(surfaceIndex);
        }

        public bool Add_D3dSurface(int surfaceIndex, IntPtr pSurface, int width, int height)
        { 
            return AddNewD3DSurface(surfaceIndex, pSurface, width, height);
        }


        public void PushCudaContext()
        {
            PushContext();
        }

        public void PopCudaContext()
        {
            PopContext();
        }

    }
}
