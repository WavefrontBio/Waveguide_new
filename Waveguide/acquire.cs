 public uint AcquireImage(int exposure, int gain, int hBinning, int vBinning, out ushort[] grayImage)
        { // exp is the exposure time in seconds

            if(!m_camera.SystemInitialized) m_camera.Initialize();

            uint uiErrorCode;
            uint ecode;
            int status = 0;
            grayImage = null;
      
            // set exposure time in seconds
            //m_camera.MyCamera.SetAcquisitionMode(1);

            ecode = m_camera.MyCamera.SetShutter(1, 1, 0, 0);
            ecode = m_camera.MyCamera.SetReadMode(4); // image mode
            ecode = m_camera.MyCamera.SetImage(hBinning, vBinning, 1, GlobalVars.PixelWidth, 1, GlobalVars.PixelHeight);
            ecode = m_camera.MyCamera.SetADChannel(0);

            ecode = m_camera.MyCamera.SetExposureTime((float)(exposure / 1000));
            ecode = m_camera.MyCamera.PrepareAcquisition();
            ecode = m_camera.MyCamera.StartAcquisition();

            while (status != 20073) //m_camera.AndorSDK.DRV_IDLE)
            {
                uiErrorCode = m_camera.MyCamera.GetStatus(ref status);
            }

            //uiErrorCode = m_camera.MyCamera.WaitForAcquisition();            

            // if good acquisition occurred, get image
            uiErrorCode = m_camera.MyCamera.GetStatus(ref status);
            if (status == 20073)
            {
                uint TotalPixels;
                TotalPixels = (uint)((GlobalVars.PixelWidth/hBinning) * (GlobalVars.PixelHeight/vBinning));
                grayImage = new ushort[TotalPixels];                
                uiErrorCode = m_camera.MyCamera.GetAcquiredData16(grayImage, TotalPixels);
                //uiErrorCode = m_camera.MyCamera.GetOldestImage16(grayImage, TotalPixels);
            }

            return uiErrorCode;
        }