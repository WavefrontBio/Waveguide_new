using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace WpfD3D
{
   
    public class SurfCollection 
    {
        struct SurfCollParams
        {
            public D3DImage d3dimage;
            public int surfaceIndex;  // surface index used by D3DSurfaceManager
            public uint width; // pixel width of D3D surface, should match image
            public uint height; // pixel height of D3D surface, should match image

            public void Clear()
            {
                d3dimage = null;
                surfaceIndex = -1;
                width = 0;
                height = 0;
            }
        }


        // dictionary to find the D3DImage by ID (some unique identifier)
        Dictionary<UInt32, SurfCollParams> m_SurfDictionary;

        IntPtr mp_D3D;
        IntPtr mp_D3D_Device;
        IntPtr mp_D3D_DeviceEx;


        // Import the methods exported by the unmanaged D3DSurfaceManager.
        [DllImport("D3DSurfaceManager.dll")]
        static extern int CreateNewSurface(uint uWidth, uint uHeight, bool useAlpha);

        [DllImport("D3DSurfaceManager.dll")]
        static extern bool DestroySurface(int SurfaceIndex);

        [DllImport("D3DSurfaceManager.dll")]
        static extern void CreateSurfaceManager();

        [DllImport("D3DSurfaceManager.dll")]
        static extern int LoadNewImage(int SurfaceIndex, IntPtr pImageData, uint width, uint height, uint numBytes);

        [DllImport("D3DSurfaceManager.dll")]
        static extern int GetBackBufferNoRef(int SurfaceIndex, out IntPtr pSurface);

        [DllImport("D3DSurfaceManager.dll")]
        static extern int Test();

        [DllImport("D3DSurfaceManager.dll")]
        static extern int GetSurfaceData(int SurfaceIndex, out IntPtr pImageData, out uint width, out uint height);

        [DllImport("D3DSurfaceManager.dll")]
        static extern void GetD3D_Objects(out IntPtr pD3D, out IntPtr pDevice, out IntPtr pDeviceEx);

        [DllImport("D3DSurfaceManager.dll")]
        static extern bool GetD3D_SurfaceParams(int SurfaceIndex, out IntPtr pSurface, out uint Width, out uint Height, out bool UseAlpha);

        [DllImport("D3DSurfaceManager.dll")]
        static extern void ReleaseSurfaceManager();


        public void Shutdown()
        {
            ReleaseSurfaceManager();
        }

        public D3DImage GetD3DImage(UInt32 id)
        {
            D3DImage d3dImage = null;  // this gets returned if no surface exists at row,col
            SurfCollParams s1;
            if (m_SurfDictionary.TryGetValue(id, out s1))
            {
                d3dImage = s1.d3dimage;
            }
            return d3dImage;
        }



        public void RemoveSurface(UInt32 id)
        {
            SurfCollParams s1;
            if (m_SurfDictionary.TryGetValue(id, out s1))
            {
                DestroySurface(s1.surfaceIndex);
                m_SurfDictionary.Remove(id);
            }
        }

        public SurfCollection()
        {         
            m_SurfDictionary = new Dictionary<uint, SurfCollParams>();

            // create the D3D Surface Manager.  The Surface Manager takes care of all the D3D stuff for you.
            CreateSurfaceManager();
        }

        public void ClearAll()
        {
            foreach (var item in m_SurfDictionary)
            {
                SurfCollParams s = (SurfCollParams)item.Value;
                DestroySurface(s.surfaceIndex);
            }
            m_SurfDictionary.Clear();
        }


        public bool AddSurface(UInt32 id, uint pixelWidth, uint pixelHeight, bool useAlpha, Image image)
        {
            bool success = false;
            SurfCollParams s1;

            if (image != null)
            {
                if (m_SurfDictionary.TryGetValue(id, out s1))
                {
                    m_SurfDictionary.Remove(id);
                    DestroySurface(s1.surfaceIndex);
                }

                s1 = new SurfCollParams();
                s1.d3dimage = new D3DImage();
                image.Source = s1.d3dimage;
                s1.width = pixelWidth;
                s1.height = pixelHeight;
                s1.surfaceIndex = CreateNewSurface(pixelWidth, pixelHeight, useAlpha);

                m_SurfDictionary.Add(id, s1);

                success = true;
            }

            return success;
        }



        public void PostNewImage(UInt32 id, byte[] data)
        {
            SurfCollParams s1;
            if (m_SurfDictionary.TryGetValue(id, out s1))
            {
                PostNewImage(s1.surfaceIndex, s1.d3dimage, s1.width, s1.height, data);
            }
        }


        void PostNewImage(int surfaceIndex, D3DImage d3dImage, uint imageWidth, uint imageHeight, byte[] data)
        {
            int size = Marshal.SizeOf(data[0]) * data.Length;

            //int cnt = data.Length;

            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(data, 0, pnt, data.Length);
            }
            finally
            {
                // Free the unmanaged memory.
                // Marshal.FreeHGlobal(pnt);
            }


            IntPtr pSurface = IntPtr.Zero;
            int result = GetBackBufferNoRef(surfaceIndex, out pSurface);  // pSurface is a pointer to a IDirect3DSurface9
            if (pSurface != IntPtr.Zero && result == 1)
            {
                try
                {
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                    LoadNewImage(surfaceIndex, pnt, imageWidth, imageHeight, (uint)data.Length);
                    d3dImage.AddDirtyRect(new Int32Rect(0, 0, (int)imageWidth, (int)imageHeight));
                    d3dImage.Unlock();
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                }
            }

            Marshal.FreeHGlobal(pnt);
        }



        public void GetD3DObjects(out IntPtr pD3D, out IntPtr pDevice, out IntPtr pDeviceEx)
        {
            mp_D3D = IntPtr.Zero;
            mp_D3D_Device = IntPtr.Zero;
            mp_D3D_DeviceEx = IntPtr.Zero;
            GetD3D_Objects(out mp_D3D, out mp_D3D_Device, out mp_D3D_DeviceEx);
            pD3D = mp_D3D;
            pDevice = mp_D3D_Device;
            pDeviceEx = mp_D3D_DeviceEx;
        }

        public bool GetSurface_Params(UInt32 id, out D3DImage d3dImage, out IntPtr pSurface, out uint width, out uint height, out bool UseAlpha)
        {
            bool success = true;
            SurfCollParams s1;
            if (m_SurfDictionary.TryGetValue(id, out s1))
            {
                GetD3D_SurfaceParams(s1.surfaceIndex, out pSurface, out width, out height, out UseAlpha);
                d3dImage = s1.d3dimage;
            }
            else
            {
                d3dImage = null;
                pSurface = IntPtr.Zero;
                width = 0;
                height = 0;
                UseAlpha = false;
                success = false;
            }
            return success;
        }




    }


}
