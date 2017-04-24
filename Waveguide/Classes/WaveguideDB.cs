using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.ComponentModel;
using System.IO;

namespace Waveguide
{
    public class WaveguideDB
    {

        public ObservableCollection<ColorModelContainer> m_colorModelList;
        public ObservableCollection<MaskContainer> m_maskList;
        public ObservableCollection<ExperimentImageContainer> m_expImageList;
        public ObservableCollection<ReferenceImageContainer> m_refImageList;
        public ObservableCollection<FilterContainer> m_filterList;
        public ObservableCollection<IndicatorContainer> m_indicatorList;
        public ObservableCollection<MethodContainer> m_methodList;
        public ObservableCollection<UserContainer> m_userList;
        public ObservableCollection<ProjectContainer> m_projectList;
        public ObservableCollection<UserProjectContainer> m_userProjectList;
        public ObservableCollection<PlateTypeContainer> m_plateTypeList;
        public ObservableCollection<CompoundPlateContainer> m_compoundPlateList;
        public ObservableCollection<ExperimentCompoundPlateContainer> m_experimentCompoundPlateList;
        public ObservableCollection<PlateContainer> m_plateList;
        public ObservableCollection<ExperimentContainer> m_experimentList;
        public ObservableCollection<ExperimentIndicatorContainer> m_expIndicatorList;
        public ObservableCollection<AnalysisContainer> m_analysisList;
        public ObservableCollection<AnalysisFrameContainer> m_analysisFrameList;
        public ObservableCollection<CameraSettingsContainer> m_cameraSettingsList;

        string m_connectionString;

        //string connectionString = "Data Source=WaveFront01;Initial Catalog=WaveguideDB;Integrated Security=True";
        //string connectionString = "Data Source=GREENWAY1\\sqlexpress;Initial Catalog=WaveguideDB;Integrated Security=True";
        string lastErrMsg = "No Error";

        int m_defaultNumberOfControlPoints = 4;


        public WaveguideDB()
        {
            m_connectionString = GlobalVars.DatabaseConnectionString;

            m_colorModelList = new ObservableCollection<ColorModelContainer>();
            m_maskList = new ObservableCollection<MaskContainer>();
            m_expImageList = new ObservableCollection<ExperimentImageContainer>();
            m_refImageList = new ObservableCollection<ReferenceImageContainer>();
            m_filterList = new ObservableCollection<FilterContainer>();
            m_indicatorList = new ObservableCollection<IndicatorContainer>();
            m_methodList = new ObservableCollection<MethodContainer>();
            m_userList = new ObservableCollection<UserContainer>();
            m_projectList = new ObservableCollection<ProjectContainer>();
            m_userProjectList = new ObservableCollection<UserProjectContainer>();
            m_plateTypeList = new ObservableCollection<PlateTypeContainer>();
            m_compoundPlateList = new ObservableCollection<CompoundPlateContainer>();
            m_experimentCompoundPlateList = new ObservableCollection<ExperimentCompoundPlateContainer>();
            m_plateList = new ObservableCollection<PlateContainer>();
            m_experimentList = new ObservableCollection<ExperimentContainer>();
            m_expIndicatorList = new ObservableCollection<ExperimentIndicatorContainer>();
            m_analysisList = new ObservableCollection<AnalysisContainer>();
            m_analysisFrameList = new ObservableCollection<AnalysisFrameContainer>();
            m_cameraSettingsList = new ObservableCollection<CameraSettingsContainer>();
        }


        public string GetLastErrorMsg()
        {
            return lastErrMsg;
        }

        public void RecordError(string errMsg)
        {
            using (StreamWriter writer = new StreamWriter("DBErrorLog.txt", true))
            {
                writer.WriteLine(DateTime.Now.ToString() + ": " + errMsg);
            }
        }


        public bool IsServerConnected()
        {
            using (var l_oConnection = new SqlConnection(m_connectionString))
            {
                try
                {
                    l_oConnection.Open();
                    return true;
                }
                catch (SqlException e)
                {
                    lastErrMsg = e.Message;
                    RecordError(e.Message);
                    return false;
                }
            }
        }



        #region ColorModel
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Color Model

        public bool GetAllColorModels()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ColorModel", con))
                {
                    m_colorModelList.Clear();
                    
                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ColorModelContainer cmContainer = new ColorModelContainer();


                            cmContainer.ColorModelID = reader.GetInt32(0);
                            cmContainer.Description = reader.GetString(1);
                            cmContainer.IsDefault = reader.GetBoolean(2);
                            cmContainer.MaxPixelValue = GlobalVars.MaxPixelValue; // later set by image to be displayed
                            cmContainer.GradientSize = 1024;  // fixed
                            cmContainer.Gain = 1.0;  // can be adjusted by GUI

                            m_colorModelList.Add(cmContainer);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message; 
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }


            if (success)
            {  // get color stops for each color model


                for (int i = 0; i < m_colorModelList.Count(); i++)
                   {
                       m_colorModelList[i].Stops = new ObservableCollection<ColorModelStopContainer>();

                        using (SqlConnection con = new SqlConnection(m_connectionString))
                        {
                            con.Open();                   

                            using (SqlCommand command = new SqlCommand("SELECT * FROM ColorModelStop WHERE ColorModelID=@cmID", con))
                            {
                                command.Parameters.Add("@cmID", System.Data.SqlDbType.Int).Value = m_colorModelList[i].ColorModelID;                            

                                try
                                {
                                    SqlDataReader reader = command.ExecuteReader();
                                    while (reader.Read())
                                    {
                                        ColorModelStopContainer cmsContainer = new ColorModelStopContainer();

                                        cmsContainer.ColorModelStopID = reader.GetInt32(0);
                                        cmsContainer.ColorModelID = reader.GetInt32(1);
                                        cmsContainer.ColorIndex = reader.GetInt32(2);
                                        cmsContainer.Red = reader.GetByte(3);
                                        cmsContainer.Green = reader.GetByte(4);
                                        cmsContainer.Blue = reader.GetByte(5);

                                        m_colorModelList[i].Stops.Add(cmsContainer);
                                    }
                                }
                                catch (Exception e)
                                {
                                    lastErrMsg = e.Message;
                                    success = false;
                                    RecordError(e.Message);
                                }
                            }                    
                        }


                        // add default number of control points to the color model
                        m_colorModelList[i].ControlPts = new ObservableCollection<ColorModelControlPointContainer>();
                        int stepValue = m_colorModelList[i].MaxPixelValue / m_defaultNumberOfControlPoints;
                        int stepIndex = m_colorModelList[i].GradientSize / m_defaultNumberOfControlPoints;
                        for (int j = 0; j < m_defaultNumberOfControlPoints; j++)
                        {
                            ColorModelControlPointContainer cc = new ColorModelControlPointContainer();
                            cc.Value = stepValue * j;
                            cc.ColorIndex = stepIndex * j;

                            if (j==m_defaultNumberOfControlPoints-1)
                            {
                                // make sure last control point is at limit of ranges
                                if(cc.Value < m_colorModelList[i].MaxPixelValue) cc.Value = m_colorModelList[i].MaxPixelValue;
                                if(cc.ColorIndex < m_colorModelList[i].GradientSize) cc.ColorIndex = m_colorModelList[i].GradientSize;
                            }

                            m_colorModelList[i].ControlPts.Add(cc);
                        }

                    }

            }

            return success;

        }



        public bool GetDefaultColorModel(out ColorModel colorModel, int maxPixelValue, int gradientSize = 1024)
        {
            colorModel = null;

            bool success = GetAllColorModels();

            if(success)
            {
                foreach(ColorModelContainer cModCont in m_colorModelList)
                {
                    if(cModCont.IsDefault)
                    {
                        colorModel = new ColorModel(cModCont,maxPixelValue,gradientSize);
                    }
                }
            }

            return success;
        }



        public bool GetColorModel(int colorModelID, out ColorModelContainer model)
        {
            bool success = true;

            model = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ColorModel WHERE ColorModelID=@cmid", con))
                {
                    command.Parameters.Add("@cmid", System.Data.SqlDbType.Int).Value = colorModelID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            model = new ColorModelContainer();

                            model.ColorModelID = reader.GetInt32(0);
                            model.Description = reader.GetString(1);
                            model.IsDefault = reader.GetBoolean(2);
                            model.MaxPixelValue = GlobalVars.MaxPixelValue;
                            model.GradientSize = 1024;
                            model.Gain = 1.0;
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }


            if (success)
            {  // get color stops for each color model
                                               
                    model.Stops = new ObservableCollection<ColorModelStopContainer>();

                    using (SqlConnection con = new SqlConnection(m_connectionString))
                    {
                        con.Open();

                        using (SqlCommand command = new SqlCommand("SELECT * FROM ColorModelStop WHERE ColorModelID=@cmID", con))
                        {
                            command.Parameters.Add("@cmID", System.Data.SqlDbType.Int).Value = model.ColorModelID;

                            try
                            {
                                SqlDataReader reader = command.ExecuteReader();
                                while (reader.Read())
                                {
                                    ColorModelStopContainer cmsContainer = new ColorModelStopContainer();

                                    cmsContainer.ColorModelStopID = reader.GetInt32(0);
                                    cmsContainer.ColorModelID = reader.GetInt32(1);
                                    cmsContainer.ColorIndex = reader.GetInt32(2);
                                    cmsContainer.Red = reader.GetByte(3);
                                    cmsContainer.Green = reader.GetByte(4);
                                    cmsContainer.Blue = reader.GetByte(5);

                                    model.Stops.Add(cmsContainer);
                                }
                            }
                            catch (Exception e)
                            {
                                lastErrMsg = e.Message;
                                success = false;
                                RecordError(e.Message);
                            }
                        }

                    }

                    // add default number of control points to the color model
                    model.ControlPts = new ObservableCollection<ColorModelControlPointContainer>();
                    int stepValue = model.MaxPixelValue / m_defaultNumberOfControlPoints;
                    int stepIndex = model.GradientSize / m_defaultNumberOfControlPoints;
                    for (int j = 0; j < m_defaultNumberOfControlPoints; j++)
                    {
                        ColorModelControlPointContainer cc = new ColorModelControlPointContainer();
                        cc.Value = stepValue * j;
                        cc.ColorIndex = stepIndex * j;

                        if (j == m_defaultNumberOfControlPoints - 1)
                        {
                            // make sure last control point is at limit of ranges
                            if (cc.Value < model.MaxPixelValue) cc.Value = model.MaxPixelValue;
                            if (cc.ColorIndex < model.GradientSize) cc.ColorIndex = model.GradientSize;
                        }

                        model.ControlPts.Add(cc);
                    }                

            }

            return success;
        }





        public bool InsertColorModel(ref ColorModelContainer model)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO ColorModel (Description,IsDefault) "
                                                            + "OUTPUT INSERTED.ColorModelID "
                                                            + "VALUES(@desc,@isdefault)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@desc", model.Description);
                        command.Parameters.AddWithValue("@isdefault", model.IsDefault);
                        
                        model.ColorModelID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }



            for (int i = 0; i < model.Stops.Count(); i++)
            {

                using (SqlConnection con = new SqlConnection(m_connectionString))
                {
                    con.Open();

                    using (SqlCommand command = new SqlCommand("INSERT INTO ColorModelStop (ColorModelID,ColorIndex,Red,Green,Blue) "
                                                                + "OUTPUT INSERTED.ColorModelStopID "
                                                                + "VALUES(@cmid,@ndx,@red,@green,@blue)"
                                                                , con))
                    {
                        try
                        {
                            command.Parameters.AddWithValue("@cmid", model.Stops[i].ColorModelID);
                            command.Parameters.AddWithValue("@ndx", model.Stops[i].ColorIndex);
                            command.Parameters.AddWithValue("@red", model.Stops[i].Red);
                            command.Parameters.AddWithValue("@green", model.Stops[i].Green);
                            command.Parameters.AddWithValue("@blue", model.Stops[i].Blue);

                            model.Stops[i].ColorModelStopID = (int)command.ExecuteScalar();
                        }
                        catch (Exception e)
                        {
                            lastErrMsg = e.Message;
                            success = false;
                            RecordError(e.Message);
                        }

                    }
                }
            }

            return success;
        }



        public bool UpdateColorModel(ColorModelContainer model)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE ColorModel SET Description=@desc,IsDefault=@isdefault" +                                                            
                                                            " WHERE ColorModelID=@modelid", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@desc", model.Description);
                        command.Parameters.AddWithValue("@isdefault", model.IsDefault);
                        command.Parameters.AddWithValue("@modelid", model.ColorModelID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }



            // Delete all current color stops from database for this model
            if (success)
            {
                using (SqlConnection con = new SqlConnection(m_connectionString))
                {
                    con.Open();

                    using (SqlCommand command = new SqlCommand("DELETE FROM ColorModelStop WHERE ColorModelID=@modelid", con))
                    {
                        try
                        {
                            command.Parameters.AddWithValue("@modelid", model.ColorModelID);

                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            lastErrMsg = e.Message;
                            success = false;
                            RecordError(e.Message);
                        }
                    }
                }
            }



            // Insert all color stops into database for given model
            if (success)
            {
                for (int i = 0; i < model.Stops.Count(); i++)
                {
                    using (SqlConnection con = new SqlConnection(m_connectionString))
                    {
                        con.Open();

                        using (SqlCommand command = new SqlCommand("INSERT INTO ColorModelStop (ColorModelID,ColorIndex,Red,Green,Blue) "
                                                                + "OUTPUT INSERTED.ID "
                                                                + "VALUES(@modelid,@ndx,@red,@green,@blue)"
                                                                , con))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@modelid", model.Stops[i].ColorModelID);
                                command.Parameters.AddWithValue("@ndx", model.Stops[i].ColorIndex);
                                command.Parameters.AddWithValue("@red", model.Stops[i].Red);
                                command.Parameters.AddWithValue("@green", model.Stops[i].Green);
                                command.Parameters.AddWithValue("@blue", model.Stops[i].Blue);

                                model.Stops[i].ColorModelStopID = (int)command.ExecuteScalar();
                            }
                            catch (Exception e)
                            {
                                lastErrMsg = e.Message;
                                success = false;
                                RecordError(e.Message);
                            }
                        }
                    }
                }
            }

            return success;
        }




        public bool DeleteColorModel(int modelID)
        {
            bool success = true;


            // delete color model 
            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM ColorModel WHERE ColorModelID=@modelid", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@modelid", modelID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            if (success)
            {   // delete all color model stops for this color model
                using (SqlConnection con = new SqlConnection(m_connectionString))
                {
                    con.Open();

                    using (SqlCommand command = new SqlCommand("DELETE FROM ColorModelStop WHERE ColorModelID=@modelid", con))
                    {
                        try
                        {
                            command.Parameters.AddWithValue("@modelid", modelID);

                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            lastErrMsg = e.Message;
                            success = false;
                            RecordError(e.Message);
                        }
                    }
                }
            }

            return success;
        }

        #endregion


        #region Mask

        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Mask

        public bool GetAllMasks()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Mask", con))
                {
                    m_maskList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            MaskContainer mContainer = new MaskContainer();

                            mContainer.MaskID = reader.GetInt32(0);
                            mContainer.Rows = reader.GetInt32(1);
                            mContainer.Cols = reader.GetInt32(2);
                            mContainer.XOffset = reader.GetInt32(3);
                            mContainer.YOffset = reader.GetInt32(4);
                            mContainer.XSize = reader.GetInt32(5);
                            mContainer.YSize = reader.GetInt32(6);
                            mContainer.XStep = reader.GetDouble(7);
                            mContainer.YStep = reader.GetDouble(8);
                            mContainer.Angle = reader.GetDouble(9);
                            mContainer.Shape = reader.GetInt32(10);                            
                            mContainer.Description = reader.GetString(11);
                            mContainer.PlateTypeID = reader.GetInt32(12);
                            mContainer.ReferenceImageID = reader.GetInt32(13);
                            mContainer.IsDefault = reader.GetBoolean(14);

                            m_maskList.Add(mContainer);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }           

            return success;
        }


        public bool GetDefaultMask(ref MaskContainer mask)
        {
            bool success = true;
            mask = null;

            if (GetAllMasks())
            {
                foreach (MaskContainer _mask in m_maskList)
                {
                    if (_mask.IsDefault)
                    {
                        mask = _mask;
                        break;
                    }
                }

                if (mask == null) success = false;
            }
            else
            {                
                success = false;
            }

            return success;
        }


        public bool GetAllMasksForPlateType(int plateTypeID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Mask WHERE PlateTypeID=@ptid", con))
                {
                    command.Parameters.Add("@ptid", System.Data.SqlDbType.Int).Value = plateTypeID;

                    m_maskList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            MaskContainer mContainer = new MaskContainer();

                            mContainer.MaskID = reader.GetInt32(0);
                            mContainer.Rows = reader.GetInt32(1);
                            mContainer.Cols = reader.GetInt32(2);
                            mContainer.XOffset = reader.GetInt32(3);
                            mContainer.YOffset = reader.GetInt32(4);
                            mContainer.XSize = reader.GetInt32(5);
                            mContainer.YSize = reader.GetInt32(6);
                            mContainer.XStep = reader.GetDouble(7);
                            mContainer.YStep = reader.GetDouble(8);
                            mContainer.Angle = reader.GetDouble(9);
                            mContainer.Shape = reader.GetInt32(10);
                            mContainer.Description = reader.GetString(11);
                            mContainer.PlateTypeID = reader.GetInt32(12);
                            mContainer.ReferenceImageID = reader.GetInt32(13);
                            mContainer.IsDefault = reader.GetBoolean(14);

                            m_maskList.Add(mContainer);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool GetMask(int maskID, out MaskContainer mask)
        {
            bool success = true;

            mask = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Mask WHERE MaskID=@mID", con))
                {
                    command.Parameters.Add("@mID", System.Data.SqlDbType.Int).Value = maskID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            mask = new MaskContainer();

                            mask.MaskID = reader.GetInt32(0);
                            mask.Rows = reader.GetInt32(1);
                            mask.Cols = reader.GetInt32(2);
                            mask.XOffset = reader.GetInt32(3);
                            mask.YOffset = reader.GetInt32(4);
                            mask.XSize = reader.GetInt32(5);
                            mask.YSize = reader.GetInt32(6);
                            mask.XStep = reader.GetDouble(7);
                            mask.YStep = reader.GetDouble(8);
                            mask.Angle = reader.GetDouble(9);
                            mask.Shape = reader.GetInt32(10);
                            mask.Description = reader.GetString(11);
                            mask.PlateTypeID = reader.GetInt32(12);
                            mask.ReferenceImageID = reader.GetInt32(13);
                            mask.IsDefault = reader.GetBoolean(14);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertMask(ref MaskContainer mask)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Mask (Rows,Cols,XOffset,YOffset,XSize,YSize,XStep,YStep,Angle,Shape,Description,PlateTypeID,ReferenceImageID,IsDefault) "
                                                            + "OUTPUT INSERTED.MaskID "
                                                            + "VALUES(@rows,@cols,@xoffset,@yoffset,@xsize,@ysize,@xstep,@ystep,@angle,@shape,@desc,@refid,@imgid,@isdef)" 
                                                            , con))
                {                    
                    try
                    {
                        command.Parameters.AddWithValue("@rows", mask.Rows);
                        command.Parameters.AddWithValue("@cols", mask.Cols);
                        command.Parameters.AddWithValue("@xoffset", mask.XOffset);
                        command.Parameters.AddWithValue("@yoffset", mask.YOffset);
                        command.Parameters.AddWithValue("@xsize", mask.XSize);
                        command.Parameters.AddWithValue("@ysize", mask.YSize);
                        command.Parameters.AddWithValue("@xstep", mask.XStep);
                        command.Parameters.AddWithValue("@ystep", mask.YStep);
                        command.Parameters.AddWithValue("@angle", mask.Angle);
                        command.Parameters.AddWithValue("@shape", mask.Shape);
                        command.Parameters.AddWithValue("@desc", mask.Description);
                        command.Parameters.AddWithValue("@refid", mask.PlateTypeID);
                        command.Parameters.AddWithValue("@imgid", mask.ReferenceImageID);
                        command.Parameters.AddWithValue("@isdef", mask.IsDefault);

                        mask.MaskID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateMask(MaskContainer mask)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Mask SET Rows=@rows,Cols=@cols,XOffset=@xoffset,YOffset=@yoffset,XSize=@xsize,YSize=@ysize," +
                                                            "XStep=@xstep,YStep=@ystep,Angle=@angle,Shape=@shape,Description=@desc,PlateTypeID=@refid,ReferenceImageID=@imgid,IsDefault=@isdef" + 
                                                            " WHERE MaskID=@maskid", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@rows", mask.Rows);
                        command.Parameters.AddWithValue("@cols", mask.Cols);
                        command.Parameters.AddWithValue("@xoffset", mask.XOffset);
                        command.Parameters.AddWithValue("@yoffset", mask.YOffset);
                        command.Parameters.AddWithValue("@xsize", mask.XSize);
                        command.Parameters.AddWithValue("@ysize", mask.YSize);
                        command.Parameters.AddWithValue("@xstep", mask.XStep);
                        command.Parameters.AddWithValue("@ystep", mask.YStep);
                        command.Parameters.AddWithValue("@angle", mask.Angle);
                        command.Parameters.AddWithValue("@shape", mask.Shape);
                        command.Parameters.AddWithValue("@desc", mask.Description);
                        command.Parameters.AddWithValue("@refid", mask.PlateTypeID);
                        command.Parameters.AddWithValue("@imgid", mask.ReferenceImageID);
                        command.Parameters.AddWithValue("@isdef", mask.IsDefault);
                        command.Parameters.AddWithValue("@maskid", mask.MaskID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteMask(int maskID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Mask WHERE MaskID=@maskid", con))
                {
                    try
                    {                        
                        command.Parameters.AddWithValue("@maskid", maskID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool MaskDescriptionAvailable(string desc, ref bool available, ref int existingMaskID)
        {
            bool success = GetAllMasks();

            available = true;
            existingMaskID = 0;

            if (success)
            {
                for (int i = 0; i < m_maskList.Count(); i++)
                {
                    if (m_maskList[i].Description == desc)
                    {
                        available = false;
                        existingMaskID = m_maskList[i].MaskID;
                    }
                }
            }

            return success;
        }



        public bool MaskExists(int maskID, ref bool exists)
        {
            MaskContainer mask;
            exists = true;

            bool success = GetMask(maskID, out mask);

            if (success)
            {
                if (mask == null) exists = false;
            }

            return success;
        }



        #endregion Mask


        #region ExperimentImage

        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Experiment Image



        public bool GetAllExperimentImages()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentImage", con))
                {
                    m_expImageList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentImageContainer cont = new ExperimentImageContainer();

                            cont.ExperimentImageID = reader.GetInt32(0);
                            cont.TimeStamp = reader.GetDateTime(1);
                            cont.ExperimentIndicatorID = reader.GetInt32(2);
                            cont.MSecs = reader.GetInt32(3);                          
                            cont.MaxPixelValue = reader.GetInt32(4);
                            cont.CompressionAlgorithm = (COMPRESSION_ALGORITHM)reader.GetInt32(5);
                            cont.FilePath = reader.GetString(6);
                                                        
                            if (File.Exists(cont.FilePath))
                                cont.ImageData = Zip.Decompress_File(cont.FilePath);
                            else 
                                cont.ImageData = null;
                   
                            m_expImageList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        


        public bool GetExperimentImage(int expImageID, out ExperimentImageContainer expImage)
        {
            bool success = true;

            expImage = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentImage WHERE ExperimentImageID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = expImageID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        expImage = new ExperimentImageContainer();

                        while (reader.Read())
                        {
                            expImage.ExperimentImageID = reader.GetInt32(0);
                            expImage.TimeStamp = reader.GetDateTime(1);
                            expImage.ExperimentIndicatorID = reader.GetInt32(2);
                            expImage.MSecs = reader.GetInt32(3);
                            expImage.MaxPixelValue = reader.GetInt32(4);
                            expImage.CompressionAlgorithm = (COMPRESSION_ALGORITHM)reader.GetInt32(5);
                            expImage.FilePath = reader.GetString(6);

                            if (File.Exists(expImage.FilePath))
                                expImage.ImageData = Zip.Decompress_File(expImage.FilePath);
                            else
                                expImage.ImageData = null;
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertExperimentImage(ref ExperimentImageContainer expImage)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO ExperimentImage (TimeStamp,ExperimentIndicatorID,MSecs,MaxPixelValue,CompressionAlgorithm,FilePath) "
                                                            + "OUTPUT INSERTED.ExperimentImageID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6)"
                                                            , con))
                {
                    try
                    {
                        SqlParameter DateTimeParam = new SqlParameter("@p1", SqlDbType.DateTime2);
                        DateTimeParam.Value = expImage.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p2", expImage.ExperimentIndicatorID);
                        command.Parameters.AddWithValue("@p3", expImage.MSecs);
                        command.Parameters.AddWithValue("@p4", expImage.MaxPixelValue);
                        command.Parameters.AddWithValue("@p5", (int)expImage.CompressionAlgorithm);
                        command.Parameters.AddWithValue("@p6", expImage.FilePath);

                        // insert into database
                        expImage.ExperimentImageID = (int)command.ExecuteScalar();

                        // write image data to file
                        Zip.Compress_File(expImage.ImageData, expImage.FilePath);
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteExperimentImage(int expImageID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM ExperimentImage WHERE ExperimentImageID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", expImageID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        #endregion


        #region ReferenceImage
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Reference Image

        public bool GetAllReferenceImages()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ReferenceImage", con))
                {
                    m_refImageList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ReferenceImageContainer cont = new ReferenceImageContainer();

                            cont.ReferenceImageID = reader.GetInt32(0);
                            cont.Width = reader.GetInt32(1);
                            cont.Height = reader.GetInt32(2);
                            cont.Depth = reader.GetInt32(3);
                            
                            cont.TimeStamp = reader.GetDateTime(5);
                            cont.NumBytes = reader.GetInt32(6);
                            cont.MaxPixelValue = reader.GetInt32(7);
                            cont.CompressionAlgorithm = (COMPRESSION_ALGORITHM)reader.GetInt32(8);
                            cont.Description = reader.GetString(9);
                            cont.Type = (REFERENCE_IMAGE_TYPE)reader.GetInt32(10);

                            byte[] buffer = new byte[cont.NumBytes];
                            buffer = (byte[])reader["ImageData"];

                            ushort[] decompressedImage;
                            if (DecompressImage(cont.CompressionAlgorithm, buffer, out decompressedImage))
                            {
                                cont.ImageData = decompressedImage;
                                cont.NumBytes = cont.ImageData.Length * cont.Depth;
                            }
                            
                            m_refImageList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }





        public bool GetReferenceImage(int refImageID, out ReferenceImageContainer refImage)
        {
            bool success = true;

            refImage = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ReferenceImage WHERE ReferenceImageID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = refImageID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            refImage = new ReferenceImageContainer();

                            refImage.ReferenceImageID = reader.GetInt32(0);
                            refImage.Width = reader.GetInt32(1);
                            refImage.Height = reader.GetInt32(2);
                            refImage.Depth = reader.GetInt32(3);
                         
                            refImage.TimeStamp = reader.GetDateTime(5);
                            refImage.NumBytes = reader.GetInt32(6);
                            refImage.MaxPixelValue = reader.GetInt32(7);
                            refImage.CompressionAlgorithm = (COMPRESSION_ALGORITHM)reader.GetInt32(8);
                            refImage.Description = reader.GetString(9);
                            refImage.Type = (REFERENCE_IMAGE_TYPE)reader.GetInt32(10);

                            byte[] buffer = new byte[refImage.NumBytes];
                            buffer = (byte[])reader["ImageData"];

                            ushort[] decompressedImage;                            
                            if (DecompressImage(refImage.CompressionAlgorithm, buffer, out decompressedImage))
                            {
                                refImage.ImageData = decompressedImage;
                                refImage.NumBytes = refImage.ImageData.Length * refImage.Depth;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetReferenceImageByType(REFERENCE_IMAGE_TYPE refType, out ReferenceImageContainer refImage)
        {
            bool success = true;

            refImage = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ReferenceImage WHERE Type=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = refType;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            refImage = new ReferenceImageContainer();

                            refImage.ReferenceImageID = reader.GetInt32(0);
                            refImage.Width = reader.GetInt32(1);
                            refImage.Height = reader.GetInt32(2);
                            refImage.Depth = reader.GetInt32(3);

                            refImage.TimeStamp = reader.GetDateTime(5);
                            refImage.NumBytes = reader.GetInt32(6);
                            refImage.MaxPixelValue = reader.GetInt32(7);
                            refImage.CompressionAlgorithm = (COMPRESSION_ALGORITHM)reader.GetInt32(8);
                            refImage.Description = reader.GetString(9);
                            refImage.Type = (REFERENCE_IMAGE_TYPE)reader.GetInt32(10);

                            byte[] buffer = new byte[refImage.NumBytes];
                            buffer = (byte[])reader["ImageData"];

                            ushort[] decompressedImage;
                            if (DecompressImage(refImage.CompressionAlgorithm, buffer, out decompressedImage))
                            {
                                refImage.ImageData = decompressedImage;
                                refImage.NumBytes = refImage.ImageData.Length * refImage.Depth;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool ReferenceImageTypeAlreadyExists(REFERENCE_IMAGE_TYPE refImgType, out bool alreadyExists)
        {
            bool success = true;

            alreadyExists = false; 
          
            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM ReferenceImage WHERE Type=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = (int)refImgType;

                    try
                    {
                        int count = (int)command.ExecuteScalar();
                        if (count > 0) alreadyExists = true;
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertReferenceImage(ref ReferenceImageContainer refImage)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO ReferenceImage (Width,Height,Depth,ImageData,TimeStamp,NumBytes,MaxPixelValue,CompressionAlgorithm,Description,Type) "
                                                            + "OUTPUT INSERTED.ReferenceImageID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)"
                                                            , con))
                {
                    try
                    {
                        byte[] imageBuffer;
                        CompressImage(refImage.CompressionAlgorithm, refImage.ImageData, out imageBuffer);

                        command.Parameters.AddWithValue("@p1", refImage.Width);
                        command.Parameters.AddWithValue("@p2", refImage.Height);
                        command.Parameters.AddWithValue("@p3", refImage.Depth);
                        command.Parameters.Add("@p4", SqlDbType.VarBinary, imageBuffer.Length).Value = imageBuffer;

                        SqlParameter DateTimeParam = new SqlParameter("@p5", SqlDbType.DateTime2);
                        DateTimeParam.Value = refImage.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p6", imageBuffer.Length);
                        command.Parameters.AddWithValue("@p7", refImage.MaxPixelValue);
                        command.Parameters.AddWithValue("@p8", refImage.CompressionAlgorithm);
                        command.Parameters.AddWithValue("@p9", refImage.Description);
                        command.Parameters.AddWithValue("@p10", refImage.Type);

                        refImage.ReferenceImageID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateReferenceImage(ReferenceImageContainer refImage)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE ReferenceImage SET Width=@p1,Height=@p2,Depth=@p3,ImageData=@p4,TimeStamp=@p5,NumBytes=@p6,MaxPixelValue=@p7,CompressionAlgorithm=@p8,Description=@p9,Type=@p10 " +                            
                                                            "WHERE ReferenceImageID=@p0", con))
                {
                    try
                    {
                        byte[] imageBuffer;
                        CompressImage(refImage.CompressionAlgorithm, refImage.ImageData, out imageBuffer);

                        command.Parameters.AddWithValue("@p1", refImage.Width);
                        command.Parameters.AddWithValue("@p2", refImage.Height);
                        command.Parameters.AddWithValue("@p3", refImage.Depth);
                        command.Parameters.Add("@p4", SqlDbType.VarBinary, imageBuffer.Length).Value = imageBuffer;

                        SqlParameter DateTimeParam = new SqlParameter("@p5", SqlDbType.DateTime2);
                        DateTimeParam.Value = refImage.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p6", imageBuffer.Length);
                        command.Parameters.AddWithValue("@p7", refImage.MaxPixelValue);
                        command.Parameters.AddWithValue("@p8", refImage.CompressionAlgorithm);
                        command.Parameters.AddWithValue("@p9", refImage.Description);
                        command.Parameters.AddWithValue("@p10", refImage.Type);

                        command.Parameters.AddWithValue("@p0", refImage.ReferenceImageID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteReferenceImage(int refImageID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM ReferenceImage WHERE ReferenceImageID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", refImageID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }



            return success;
        }


        public bool ClearReferenceImageType(REFERENCE_IMAGE_TYPE refImgType)
        {
            bool success = true;

            //ReferenceImageContainer refImage;
            //success = GetReferenceImageByType(refImgType, out refImage);
            //if (success && refImage != null)
            //{
                using (SqlConnection con = new SqlConnection(m_connectionString))
                {
                    con.Open();

                    using (SqlCommand command = new SqlCommand("UPDATE ReferenceImage SET Type=0 WHERE Type=@p1", con))
                    {
                        try
                        {
                            command.Parameters.AddWithValue("@p1", refImgType);

                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            lastErrMsg = e.Message;
                            success = false;
                            RecordError(e.Message);
                        }
                    }
                }
            //}


            return success;
        }



        public bool ReferenceImageExists(int refImageID, ref bool exists)
        {
            exists = true;
            ReferenceImageContainer refImage;

            bool success = GetReferenceImage(refImageID, out refImage);

            if (success)
            {
                if (refImage == null)
                {
                    exists = false;
                }
            }
            
            return success;
        }



        #endregion


        #region Filter
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Filter


        public bool GetAllFilters()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter", con))
                {
                    m_filterList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            FilterContainer cont = new FilterContainer();

                            cont.FilterID = reader.GetInt32(0);
                            cont.FilterChanger = reader.GetInt32(1);
                            cont.PositionNumber = reader.GetInt32(2);
                            cont.Description = reader.GetString(3);
                            cont.Manufacturer = reader.GetString(4);
                            cont.PartNumber = reader.GetString(5);

                            m_filterList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllExcitationFilters()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter WHERE FilterChanger=1", con))
                {
                    m_filterList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            FilterContainer cont = new FilterContainer();

                            cont.FilterID = reader.GetInt32(0);
                            cont.FilterChanger = reader.GetInt32(1);
                            cont.PositionNumber = reader.GetInt32(2);
                            cont.Description = reader.GetString(3);
                            cont.Manufacturer = reader.GetString(4);
                            cont.PartNumber = reader.GetString(5);

                            m_filterList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllEmissionFilters()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter WHERE FilterChanger=0", con))
                {
                    m_filterList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            FilterContainer cont = new FilterContainer();

                            cont.FilterID = reader.GetInt32(0);
                            cont.FilterChanger = reader.GetInt32(1);
                            cont.PositionNumber = reader.GetInt32(2);
                            cont.Description = reader.GetString(3);
                            cont.Manufacturer = reader.GetString(4);
                            cont.PartNumber = reader.GetString(5);

                            m_filterList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetFilter(int filterID, out FilterContainer filter)
        {
            bool success = true;

            filter = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter WHERE FilterID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = filterID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            filter = new FilterContainer();

                            filter.FilterID = reader.GetInt32(0);
                            filter.FilterChanger = reader.GetInt32(1);
                            filter.PositionNumber = reader.GetInt32(2);
                            filter.Description = reader.GetString(3);
                            filter.Manufacturer = reader.GetString(4);
                            filter.PartNumber = reader.GetString(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetExcitationFilterAtPosition(int positionNumber, out FilterContainer filter)
        {
            bool success = true;

            filter = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter WHERE FilterChanger=1 AND PositionNumber=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = positionNumber;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            filter = new FilterContainer();

                            filter.FilterID = reader.GetInt32(0);
                            filter.FilterChanger = reader.GetInt32(1);
                            filter.PositionNumber = reader.GetInt32(2);
                            filter.Description = reader.GetString(3);
                            filter.Manufacturer = reader.GetString(4);
                            filter.PartNumber = reader.GetString(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetEmissionFilterAtPosition(int positionNumber, out FilterContainer filter)
        {
            bool success = true;

            filter = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Filter WHERE FilterChanger=0 AND PositionNumber=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = positionNumber;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            filter = new FilterContainer();

                            filter.FilterID = reader.GetInt32(0);
                            filter.FilterChanger = reader.GetInt32(1);
                            filter.PositionNumber = reader.GetInt32(2);
                            filter.Description = reader.GetString(3);
                            filter.Manufacturer = reader.GetString(4);
                            filter.PartNumber = reader.GetString(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertFilter(ref FilterContainer filter)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Filter (FilterChanger,PositionNumber,Description,Manufacturer,PartNumber) "
                                                            + "OUTPUT INSERTED.FilterID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", filter.FilterChanger);
                        command.Parameters.AddWithValue("@p2", filter.PositionNumber);
                        command.Parameters.AddWithValue("@p3", filter.Description);
                        command.Parameters.AddWithValue("@p4", filter.Manufacturer);
                        command.Parameters.AddWithValue("@p5", filter.PartNumber);

                        filter.FilterID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateFilter(FilterContainer filter)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Filter SET FilterChanger=@p1,PositionNumber=@p2,Description=@p3,Manufacturer=@p4,PartNumber=@p5 " +                                                            
                                                            "WHERE FilterID=@p6", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", filter.FilterChanger);
                        command.Parameters.AddWithValue("@p2", filter.PositionNumber);
                        command.Parameters.AddWithValue("@p3", filter.Description);
                        command.Parameters.AddWithValue("@p4", filter.Manufacturer);
                        command.Parameters.AddWithValue("@p5", filter.PartNumber);
                        command.Parameters.AddWithValue("@p6", filter.FilterID);                       

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteFilter(int filterID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Filter WHERE FilterID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", filterID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool DeleteAllFilters()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Filter", con))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        #endregion


        #region Indicator
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Indicator

        public bool GetAllIndicators()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Indicator", con))
                {
                    m_indicatorList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            IndicatorContainer cont = new IndicatorContainer();

                            cont.IndicatorID = reader.GetInt32(0);
                            cont.MethodID = reader.GetInt32(1);
                            cont.ExcitationFilterPosition = reader.GetInt32(2);
                            cont.EmissionsFilterPosition = reader.GetInt32(3);
                            cont.Description = reader.GetString(4);
                            cont.SignalType = (SIGNAL_TYPE)reader.GetInt32(5);

                            m_indicatorList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllIndicatorsForMethod(int methodID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Indicator WHERE MethodID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = methodID;

                    m_indicatorList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            IndicatorContainer cont = new IndicatorContainer();

                            cont.IndicatorID = reader.GetInt32(0);
                            cont.MethodID = reader.GetInt32(1);
                            cont.ExcitationFilterPosition = reader.GetInt32(2);
                            cont.EmissionsFilterPosition = reader.GetInt32(3);
                            cont.Description = reader.GetString(4);
                            cont.SignalType = (SIGNAL_TYPE)reader.GetInt32(5);

                            m_indicatorList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }





        public bool GetIndicator(int indicatorID, out IndicatorContainer indicator)
        {
            bool success = true;

            indicator = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Indicator WHERE IndicatorID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = indicatorID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            indicator = new IndicatorContainer();

                            indicator.IndicatorID = reader.GetInt32(0);
                            indicator.MethodID = reader.GetInt32(1);
                            indicator.ExcitationFilterPosition = reader.GetInt32(2);
                            indicator.EmissionsFilterPosition = reader.GetInt32(3);
                            indicator.Description = reader.GetString(4);
                            indicator.SignalType = (SIGNAL_TYPE)reader.GetInt32(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertIndicator(ref IndicatorContainer indicator)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Indicator (MethodID,ExcitationFilterPosition,EmissionsFilterPosition,Description,SignalType) "
                                                            + "OUTPUT INSERTED.IndicatorID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", indicator.MethodID);
                        command.Parameters.AddWithValue("@p2", indicator.ExcitationFilterPosition);
                        command.Parameters.AddWithValue("@p3", indicator.EmissionsFilterPosition);
                        command.Parameters.AddWithValue("@p4", indicator.Description);
                        command.Parameters.AddWithValue("@p5", (int)indicator.SignalType);

                        indicator.IndicatorID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateIndicator(IndicatorContainer indicator)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Indicator SET MethodID=@p1,ExcitationFilterPosition=@p2,EmissionsFilterPosition=@p3,Description=@p4,SignalType=@p5 " +
                                                            "WHERE IndicatorID=@p6", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", indicator.MethodID);
                        command.Parameters.AddWithValue("@p2", indicator.ExcitationFilterPosition);
                        command.Parameters.AddWithValue("@p3", indicator.EmissionsFilterPosition);
                        command.Parameters.AddWithValue("@p4", indicator.Description);
                        command.Parameters.AddWithValue("@p5", (int)indicator.SignalType);
                        command.Parameters.AddWithValue("@p6", indicator.IndicatorID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteIndicator(int indicatorID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Indicator WHERE IndicatorID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", indicatorID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false; 
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        #endregion


        #region Method
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Method


        public bool GetAllMethods()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Method", con))
                {
                    m_methodList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            MethodContainer cont = new MethodContainer();

                            cont.MethodID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.BravoMethodFile = reader.GetString(2);
                            cont.OwnerID = reader.GetInt32(3);
                            cont.IsPublic = reader.GetBoolean(4);
                          
                            m_methodList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllMethodsForUser(int userID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Method WHERE OwnerID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = userID;

                    m_methodList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            MethodContainer cont = new MethodContainer();

                            cont.MethodID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.BravoMethodFile = reader.GetString(2);
                            cont.OwnerID = reader.GetInt32(3);
                            cont.IsPublic = reader.GetBoolean(4);

                            m_methodList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetAllPublicMethods()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Method WHERE IsPublic=1", con))
                {
                    m_methodList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            MethodContainer cont = new MethodContainer();

                            cont.MethodID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.BravoMethodFile = reader.GetString(2);
                            cont.OwnerID = reader.GetInt32(3);
                            cont.IsPublic = reader.GetBoolean(4);

                            m_methodList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool GetMethod(int methodID, out MethodContainer method)
        {
            bool success = true;

            method = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Method WHERE MethodID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = methodID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            method = new MethodContainer();

                            method.MethodID = reader.GetInt32(0);
                            method.Description = reader.GetString(1);
                            method.BravoMethodFile = reader.GetString(2);
                            method.OwnerID = reader.GetInt32(3);
                            method.IsPublic = reader.GetBoolean(4);                          
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertMethod(ref MethodContainer method)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Method (Description,BravoMethodFile,OwnerID,IsPublic) "
                                                            + "OUTPUT INSERTED.MethodID "
                                                            + "VALUES(@p1,@p2,@p3,@p4)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", method.Description);
                        command.Parameters.AddWithValue("@p2", method.BravoMethodFile);
                        command.Parameters.AddWithValue("@p3", method.OwnerID);
                        command.Parameters.AddWithValue("@p4", method.IsPublic);                        

                        method.MethodID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateMethod(MethodContainer method)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Method SET Description=@p1,BravoMethodFile=@p2,OwnerID=@p3,IsPublic=@p4 " +
                                                            "WHERE MethodID=@p5", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", method.Description);
                        command.Parameters.AddWithValue("@p2", method.BravoMethodFile);
                        command.Parameters.AddWithValue("@p3", method.OwnerID);
                        command.Parameters.AddWithValue("@p4", method.IsPublic);
                        command.Parameters.AddWithValue("@p5", method.MethodID);                       

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteMethod(int methodID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Method WHERE MethodID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", methodID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        #endregion
        

        #region User
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // User

        // NOTE:  Have to put the User table name in brackets because "User" is a reserved word in SQL Server

        public bool GetAllUsers()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM [User]", con))
                {
                    m_userList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            UserContainer cont = new UserContainer();

                            cont.UserID = reader.GetInt32(0);
                            cont.Firstname = reader.GetString(1);
                            cont.Lastname = reader.GetString(2);
                            cont.Username = reader.GetString(3);
                            cont.Password = reader.GetString(4);
                            cont.Role = (GlobalVars.USER_ROLE_ENUM)reader.GetInt32(5);

                            m_userList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }





        public bool GetUser(int userID, out UserContainer user)
        {
            bool success = true;

            user = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM [User] WHERE UserID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = userID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            user = new UserContainer();

                            user.UserID = reader.GetInt32(0);
                            user.Firstname = reader.GetString(1);
                            user.Lastname = reader.GetString(2);
                            user.Username = reader.GetString(3);
                            user.Password = reader.GetString(4);
                            user.Role = (GlobalVars.USER_ROLE_ENUM)reader.GetInt32(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool GetUserByUsername(string username, out UserContainer user)
        {
            bool success = true;

            user = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM [User] WHERE Username=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.NVarChar).Value = username;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            user = new UserContainer();

                            user.UserID = reader.GetInt32(0);
                            user.Firstname = reader.GetString(1);
                            user.Lastname = reader.GetString(2);
                            user.Username = reader.GetString(3);
                            user.Password = reader.GetString(4);
                            user.Role = (GlobalVars.USER_ROLE_ENUM)reader.GetInt32(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool InsertUser(ref UserContainer user)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO [User] (Firstname,Lastname,Username,Password,Role) "
                                                            + "OUTPUT INSERTED.UserID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", user.Firstname);
                        command.Parameters.AddWithValue("@p2", user.Lastname);
                        command.Parameters.AddWithValue("@p3", user.Username);
                        command.Parameters.AddWithValue("@p4", user.Password);
                        command.Parameters.AddWithValue("@p5", user.Role);

                        user.UserID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateUser(UserContainer user)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE [User] SET Firstname=@p1,Lastname=@p2,Username=@p3,Password=@p4,Role=@p5 " +
                                                            "WHERE UserID=@p6", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", user.Firstname);
                        command.Parameters.AddWithValue("@p2", user.Lastname);
                        command.Parameters.AddWithValue("@p3", user.Username);
                        command.Parameters.AddWithValue("@p4", user.Password);
                        command.Parameters.AddWithValue("@p5", user.Role);
                        command.Parameters.AddWithValue("@p6", user.UserID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteUser(int userID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM [User] WHERE UserID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", userID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }
        
        #endregion


        #region Project
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Project


        public bool GetAllProjects(bool IncludeArchived)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                string sqlStatement = "SELECT * FROM Project WHERE Archived=0";

                if (IncludeArchived) sqlStatement = "SELECT * FROM Project";

                using (SqlCommand command = new SqlCommand(sqlStatement, con))
                {
                    m_projectList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ProjectContainer cont = new ProjectContainer();

                            cont.ProjectID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.Archived = reader.GetBoolean(2);
                            cont.TimeStamp = reader.GetDateTime(3);
                           
                            m_projectList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }





        public bool GetProject(int projectID, out ProjectContainer project)
        {
            bool success = true;

            project = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Project WHERE ProjectID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = projectID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            project = new ProjectContainer();

                            project.ProjectID = reader.GetInt32(0);
                            project.Description = reader.GetString(1);
                            project.Archived = reader.GetBoolean(2);
                            project.TimeStamp = reader.GetDateTime(3);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertProject(ref ProjectContainer project)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Project (Description,Archived,TimeStamp) "
                                                            + "OUTPUT INSERTED.ProjectID "
                                                            + "VALUES(@p1,@p2,@p3)"
                                                            , con))
                {
                    try
                    {                       

                        command.Parameters.AddWithValue("@p1", project.Description);
                        command.Parameters.AddWithValue("@p2", project.Archived);

                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = project.TimeStamp;
                        command.Parameters.Add(DateTimeParam);
                                                
                        project.ProjectID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateProject(ProjectContainer project)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Project SET Description=@p1,Archived=@p2,TimeStamp=@p3 " +
                                                            "WHERE ProjectID=@p4", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", project.Description);
                        command.Parameters.AddWithValue("@p2", project.Archived);

                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = project.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p4", project.ProjectID);
                                                
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteProject(int projectID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Project WHERE ProjectID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", projectID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }

        #endregion


        #region UserProject
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // UserProject


        public bool GetAllUserProjects()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM UserProject", con))
                {
                    m_projectList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            UserProjectContainer cont = new UserProjectContainer();

                            cont.UserID = reader.GetInt32(0);
                            cont.ProjectID = reader.GetInt32(1);

                            m_userProjectList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetAllProjectsForUser(int userID, out ObservableCollection<ProjectContainer> projectList)
        {
            bool success = true;

            projectList = new ObservableCollection<ProjectContainer>(); 

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM UserProject WHERE UserID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = userID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            UserProjectContainer up = new UserProjectContainer();
                            up.UserID = reader.GetInt32(0);
                            up.ProjectID = reader.GetInt32(1);
                            
                            ProjectContainer cont = new ProjectContainer();
                            success = GetProject(up.ProjectID, out cont);
                            if (success)
                            {
                                projectList.Add(cont);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllUsersForProject(int projectID, out List<UserContainer> userList)
        {
            bool success = true;

            userList = new List<UserContainer>();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM UserProject WHERE ProjectID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = projectID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {

                            UserProjectContainer up = new UserProjectContainer();
                            up.UserID = reader.GetInt32(0);
                            up.ProjectID = reader.GetInt32(1);
                                                        
                            UserContainer cont = new UserContainer();
                            success = GetUser(up.UserID, out cont);
                            if (success)
                            {
                                userList.Add(cont);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        

        public bool AddUserToProject(int userID, int projectID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("IF NOT EXISTS (SELECT * FROM UserProject WHERE UserID=@p1 AND ProjectID=@p2) "
                                                            + "INSERT INTO UserProject (UserID,ProjectID) "                                                            
                                                            + "VALUES(@p1,@p2)"
                                                            , con))               
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", userID);
                        command.Parameters.AddWithValue("@p2", projectID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool RemoveUserFromProject(int userID, int projectID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM UserProject WHERE UserID=@p1 AND ProjectID=@p2", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", userID);
                        command.Parameters.AddWithValue("@p2", projectID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool RemoveUserFromUserProjectTable(int userID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM UserProject WHERE UserID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", userID);                        

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool RemoveProjectFromUserProjectTable(int projectID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM UserProject WHERE ProjectID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", projectID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool IsUserAssignedToProject(int userID, int projectID, ref bool IsAssigned)
        {
            bool success = true;
            IsAssigned = false;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM UserProject WHERE UserID=@p1 AND ProjectID=@p2", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", userID);
                        command.Parameters.AddWithValue("@p2", projectID);

                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            // found a record;
                            IsAssigned = true;
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        #endregion


        #region PlateType
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // PlateType


        public bool GetAllPlateTypes()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM PlateType", con))
                {
                    m_plateTypeList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            PlateTypeContainer cont = new PlateTypeContainer();

                            cont.PlateTypeID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.Rows = reader.GetInt32(2);
                            cont.Cols = reader.GetInt32(3);
                            cont.IsDefault = reader.GetBoolean(4);

                            m_plateTypeList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetPlateType(int platetypeID, out PlateTypeContainer platetype)
        {
            bool success = true;

            platetype = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM PlateType WHERE PlateTypeID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = platetypeID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            platetype = new PlateTypeContainer();

                            platetype.PlateTypeID = reader.GetInt32(0);
                            platetype.Description = reader.GetString(1);
                            platetype.Rows = reader.GetInt32(2);
                            platetype.Cols = reader.GetInt32(3);
                            platetype.IsDefault = reader.GetBoolean(4);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertPlateType(ref PlateTypeContainer platetype)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO PlateType (Description,Rows,Cols,IsDefault) "
                                                            + "OUTPUT INSERTED.PlateTypeID "
                                                            + "VALUES(@p1,@p2,@p3,@p4)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", platetype.Description);
                        command.Parameters.AddWithValue("@p2", platetype.Rows);
                        command.Parameters.AddWithValue("@p3", platetype.Cols);
                        command.Parameters.AddWithValue("@p4", platetype.IsDefault);

                        platetype.PlateTypeID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdatePlateType(PlateTypeContainer platetype)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE PlateType SET Description=@p1,Rows=@p2,Cols=@p3,IsDefault=@p4 " +
                                                            "WHERE PlateTypeID=@p5", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", platetype.Description);
                        command.Parameters.AddWithValue("@p2", platetype.Rows);
                        command.Parameters.AddWithValue("@p3", platetype.Cols);
                        command.Parameters.AddWithValue("@p4", platetype.IsDefault);
                        command.Parameters.AddWithValue("@p5", platetype.PlateTypeID);                        

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeletePlateType(int platetypeID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM PlateType WHERE PlateTypeID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", platetypeID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }

        #endregion


        #region CompoundPlate
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // CompoundPlate


        public bool GetAllCompoundPlatesForMethod(int methodID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM CompoundPlate WHERE MethodID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = methodID;

                    m_compoundPlateList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            CompoundPlateContainer cont = new CompoundPlateContainer();

                            cont.CompoundPlateID = reader.GetInt32(0);
                            cont.MethodID = reader.GetInt32(1);
                            cont.Description = reader.GetString(2);
                            
                            m_compoundPlateList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        public bool GetCompoundPlate(int plateID, out CompoundPlateContainer plate)
        {
            bool success = true;

            plate = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM CompoundPlate WHERE CompoundPlateID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = plateID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            plate = new CompoundPlateContainer();

                            plate.CompoundPlateID = reader.GetInt32(0);
                            plate.MethodID = reader.GetInt32(1);
                            plate.Description = reader.GetString(2);                            
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertCompoundPlate(ref CompoundPlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO CompoundPlate (MethodID,Description) "
                                                            + "OUTPUT INSERTED.CompoundPlateID "
                                                            + "VALUES(@p1,@p2)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.MethodID);
                        command.Parameters.AddWithValue("@p2", plate.Description);

                        plate.CompoundPlateID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateCompoundPlate(CompoundPlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE CompoundPlate SET MethodID=@p1,Description=@p2 " +
                                                            "WHERE CompoundPlateID=@p3", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.MethodID);
                        command.Parameters.AddWithValue("@p2", plate.Description);                        
                        command.Parameters.AddWithValue("@p3", plate.CompoundPlateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteCompoundPlate(int plateID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM CompoundPlate WHERE CompoundPlateID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }

        #endregion
        

        #region ExperimentCompoundPlate
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // ExperimentCompoundPlate


        public bool GetAllExperimentCompoundPlates()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentCompoundPlate", con))
                {
                    m_experimentCompoundPlateList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentCompoundPlateContainer cont = new ExperimentCompoundPlateContainer();

                            cont.ExperimentCompoundPlateID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.Barcode = reader.GetString(2);
                            cont.ExperimentID = reader.GetInt32(3);

                            m_experimentCompoundPlateList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }






        public bool GetAllExperimentCompoundPlatesForExperiment(int experimentID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentCompoundPlate WHERE ExperimentID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = experimentID;

                    m_experimentCompoundPlateList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentCompoundPlateContainer cont = new ExperimentCompoundPlateContainer();

                            cont.ExperimentCompoundPlateID = reader.GetInt32(0);
                            cont.Description = reader.GetString(1);
                            cont.Barcode = reader.GetString(2);
                            cont.ExperimentID = reader.GetInt32(3);

                            m_experimentCompoundPlateList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false; 
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }






        public bool GetExperimentCompoundPlate(int plateID, out ExperimentCompoundPlateContainer plate)
        {
            bool success = true;

            plate = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentCompoundPlate WHERE ExperimentCompoundPlateID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = plateID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            plate = new ExperimentCompoundPlateContainer();

                            plate.ExperimentCompoundPlateID = reader.GetInt32(0);
                            plate.Description = reader.GetString(1);
                            plate.Barcode = reader.GetString(2);
                            plate.ExperimentID = reader.GetInt32(3);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetExperimentCompoundPlateByBarcode(string barcode, out ExperimentCompoundPlateContainer plate)
        {
            bool success = true;

            plate = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentCompoundPlate WHERE Barcode=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.NVarChar).Value = barcode;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            plate = new ExperimentCompoundPlateContainer();

                            plate.ExperimentCompoundPlateID = reader.GetInt32(0);
                            plate.Description = reader.GetString(1);
                            plate.Barcode = reader.GetString(2);
                            plate.ExperimentID = reader.GetInt32(3);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertExperimentCompoundPlate(ref ExperimentCompoundPlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO ExperimentCompoundPlate (Description,Barcode,ExperimentID) "
                                                            + "OUTPUT INSERTED.ExperimentCompoundPlateID "
                                                            + "VALUES(@p1,@p2,@p3)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.Description);
                        command.Parameters.AddWithValue("@p2", plate.Barcode);
                        command.Parameters.AddWithValue("@p3", plate.ExperimentID);

                        plate.ExperimentCompoundPlateID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateExperimentCompoundPlate(ExperimentCompoundPlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE ExperimentCompoundPlate SET Description=@p1,Barcode=@p2,ExperimentID=@p3 " +
                                                            "WHERE ExperimentCompoundPlateID=@p4", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.Description);
                        command.Parameters.AddWithValue("@p2", plate.Barcode);
                        command.Parameters.AddWithValue("@p3", plate.ExperimentID);
                        command.Parameters.AddWithValue("@p4", plate.ExperimentCompoundPlateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteExperimentCompoundPlate(int plateID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM ExperimentCompoundPlate WHERE ExperimentCompoundPlateID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }

        #endregion
        

        #region Plate
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Plate


        public bool GetAllPlates()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Plate", con))
                {
                    m_plateList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            PlateContainer cont = new PlateContainer();

                            cont.PlateID = reader.GetInt32(0);
                            cont.ProjectID = reader.GetInt32(1);
                            cont.OwnerID = reader.GetInt32(2);
                            cont.Barcode = reader.GetString(3);
                            cont.PlateTypeID = reader.GetInt32(4);
                            cont.Description = reader.GetString(5);
                            cont.IsPublic = reader.GetBoolean(6);

                            m_plateList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetAllPlatesForProject(int projectID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Plate WHERE ProjectID=@p1", con))
                {
                    m_plateList.Clear();

                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = projectID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            PlateContainer cont = new PlateContainer();

                            cont.PlateID = reader.GetInt32(0);                            
                            cont.ProjectID = reader.GetInt32(1);
                            cont.OwnerID = reader.GetInt32(2);
                            cont.Barcode = reader.GetString(3);
                            cont.PlateTypeID = reader.GetInt32(4);
                            cont.Description = reader.GetString(5);
                            cont.IsPublic = reader.GetBoolean(6);

                            m_plateList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllPlatesForProjectAndUser(int projectID, int userID, out ObservableCollection<PlateContainer> plateList)
        {
            bool success = true;

            plateList = new ObservableCollection<PlateContainer>();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Plate WHERE ProjectID=@p1", con))
                {
                    m_plateList.Clear();

                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = projectID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            PlateContainer cont = new PlateContainer();

                            cont.PlateID = reader.GetInt32(0);
                            cont.ProjectID = reader.GetInt32(1);
                            cont.OwnerID = reader.GetInt32(2);
                            cont.Barcode = reader.GetString(3);
                            cont.PlateTypeID = reader.GetInt32(4);
                            cont.Description = reader.GetString(5);
                            cont.IsPublic = reader.GetBoolean(6);

                            if (cont.OwnerID == userID)
                            {
                                m_plateList.Add(cont);
                            }
                            else if (cont.IsPublic)
                            {
                                m_plateList.Add(cont);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetPlateByBarcode(string barcode, out PlateContainer plate)
        {
            bool success = true;

            plate = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Plate WHERE Barcode=@p1", con))
                {
                    m_plateList.Clear();

                    command.Parameters.Add("@p1", System.Data.SqlDbType.NVarChar).Value = barcode;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            plate = new PlateContainer();

                            plate.PlateID = reader.GetInt32(0);
                            plate.ProjectID = reader.GetInt32(1);
                            plate.OwnerID = reader.GetInt32(2);
                            plate.Barcode = reader.GetString(3);
                            plate.PlateTypeID = reader.GetInt32(4);
                            plate.Description = reader.GetString(5);
                            plate.IsPublic = reader.GetBoolean(6);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetPlate(int plateID, out PlateContainer plate)
        {
            bool success = true;

            plate = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Plate WHERE PlateID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = plateID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            plate = new PlateContainer();

                            plate.PlateID = reader.GetInt32(0);
                            plate.ProjectID = reader.GetInt32(1);
                            plate.OwnerID = reader.GetInt32(2);
                            plate.Barcode = reader.GetString(3);
                            plate.PlateTypeID = reader.GetInt32(4);
                            plate.Description = reader.GetString(5);
                            plate.IsPublic = reader.GetBoolean(6);                           
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertPlate(ref PlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Plate (ProjectID,OwnerID,Barcode,PlateTypeID,Description,IsPublic) "
                                                            + "OUTPUT INSERTED.PlateID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.ProjectID);
                        command.Parameters.AddWithValue("@p2", plate.OwnerID);
                        command.Parameters.AddWithValue("@p3", plate.Barcode);
                        command.Parameters.AddWithValue("@p4", plate.PlateTypeID);
                        command.Parameters.AddWithValue("@p5", plate.Description);
                        command.Parameters.AddWithValue("@p6", plate.IsPublic);

                        plate.PlateID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdatePlate(PlateContainer plate)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Plate SET ProjectID=@p1,OwnerID=@p2,Barcode=@p3,PlateTypeID=@p4,Description=@p5,IsPublic=@p6 " +
                                                            "WHERE PlateID=@p7", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plate.ProjectID);
                        command.Parameters.AddWithValue("@p2", plate.OwnerID);
                        command.Parameters.AddWithValue("@p3", plate.Barcode);
                        command.Parameters.AddWithValue("@p4", plate.PlateTypeID);
                        command.Parameters.AddWithValue("@p5", plate.Description);
                        command.Parameters.AddWithValue("@p6", plate.IsPublic);
                        command.Parameters.AddWithValue("@p7", plate.PlateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeletePlate(int plateID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Plate WHERE PlateID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", plateID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        #endregion
        

        #region Experiment
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Experiment


        public bool GetAllExperiments()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Experiment", con))
                {
                    m_experimentList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentContainer cont = new ExperimentContainer();

                            cont.ExperimentID = reader.GetInt32(0);
                            cont.PlateID = reader.GetInt32(1);
                            cont.MethodID = reader.GetInt32(2);
                            cont.TimeStamp = reader.GetDateTime(3);
                            cont.Description = reader.GetString(4);
                            cont.HorzBinning = reader.GetInt32(5);
                            cont.VertBinning = reader.GetInt32(6);
                            cont.ROI_Origin_X = reader.GetInt32(7);
                            cont.ROI_Origin_Y = reader.GetInt32(8);
                            cont.ROI_Width = reader.GetInt32(9);
                            cont.ROI_Height = reader.GetInt32(10);

                            m_experimentList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllExperimentsForPlate(int plateID, out ObservableCollection<ExperimentContainer> experimentList)
        {
            bool success = true;

            experimentList = new ObservableCollection<ExperimentContainer>();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Experiment WHERE PlateID=@p1", con))
                {
                    experimentList.Clear();

                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = plateID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentContainer cont = new ExperimentContainer();

                            cont.ExperimentID = reader.GetInt32(0);
                            cont.PlateID = reader.GetInt32(1);
                            cont.MethodID = reader.GetInt32(2);
                            cont.TimeStamp = reader.GetDateTime(3);
                            cont.Description = reader.GetString(4);
                            cont.HorzBinning = reader.GetInt32(5);
                            cont.VertBinning = reader.GetInt32(6);
                            cont.ROI_Origin_X = reader.GetInt32(7);
                            cont.ROI_Origin_Y = reader.GetInt32(8);
                            cont.ROI_Width = reader.GetInt32(9);
                            cont.ROI_Height = reader.GetInt32(10);

                            experimentList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetExperiment(int experimentID, out ExperimentContainer experiment)
        {
            bool success = true;

            experiment = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Experiment WHERE ExperimentID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = experimentID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            experiment = new ExperimentContainer();

                            experiment.ExperimentID = reader.GetInt32(0);
                            experiment.PlateID = reader.GetInt32(1);
                            experiment.MethodID = reader.GetInt32(2);
                            experiment.TimeStamp = reader.GetDateTime(3);
                            experiment.Description = reader.GetString(4);
                            experiment.HorzBinning = reader.GetInt32(5);
                            experiment.VertBinning = reader.GetInt32(6);
                            experiment.ROI_Origin_X = reader.GetInt32(7);
                            experiment.ROI_Origin_Y = reader.GetInt32(8);
                            experiment.ROI_Width = reader.GetInt32(9);
                            experiment.ROI_Height = reader.GetInt32(10);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertExperiment(ref ExperimentContainer experiment)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Experiment (PlateID,MethodID,TimeStamp,Description,HorzBinning,VertBinning,ROI_Origin_X,ROI_Origin_Y,ROI_Width,ROI_Height) "
                                                            + "OUTPUT INSERTED.ExperimentID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", experiment.PlateID);
                        command.Parameters.AddWithValue("@p2", experiment.MethodID);

                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = experiment.TimeStamp;
                        command.Parameters.Add(DateTimeParam);                        

                        command.Parameters.AddWithValue("@p4", experiment.Description);
                        command.Parameters.AddWithValue("@p5", experiment.HorzBinning);
                        command.Parameters.AddWithValue("@p6", experiment.VertBinning);
                        command.Parameters.AddWithValue("@p7", experiment.ROI_Origin_X);
                        command.Parameters.AddWithValue("@p8", experiment.ROI_Origin_Y);
                        command.Parameters.AddWithValue("@p9", experiment.ROI_Width);
                        command.Parameters.AddWithValue("@p10", experiment.ROI_Height);

                        experiment.ExperimentID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateExperiment(ExperimentContainer experiment)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Experiment SET PlateID=@p1,MethodID=@p2,TimeStamp=@p3,Description=@p4,HorzBinning=@p5,VertBinning=@p6," +
                                                           "ROI_Origin_X=@p7,ROI_Origin_Y=@p8,ROI_Width=@p9,ROI_Height=@p10 " +
                                                            "WHERE ExperimentID=@p11", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1",experiment.PlateID); 
                        command.Parameters.AddWithValue("@p2",experiment.MethodID);

                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = experiment.TimeStamp;
                        command.Parameters.Add(DateTimeParam);
                        
                        command.Parameters.AddWithValue("@p4",experiment.Description); 
                        command.Parameters.AddWithValue("@p5",experiment.HorzBinning); 
                        command.Parameters.AddWithValue("@p6",experiment.VertBinning);
                        command.Parameters.AddWithValue("@p7",experiment.ROI_Origin_X);
                        command.Parameters.AddWithValue("@p8",experiment.ROI_Origin_Y);
                        command.Parameters.AddWithValue("@p9",experiment.ROI_Width);
                        command.Parameters.AddWithValue("@p10",experiment.ROI_Height);
                        command.Parameters.AddWithValue("@p11", experiment.ExperimentID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteExperiment(int experimentID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM Experiment WHERE ExperimentID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", experimentID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        #endregion
        

        #region ExperimentIndicator
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Experiment Indicator


        public bool GetAllExperimentIndicators()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentIndicator", con))
                {
                    m_expIndicatorList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentIndicatorContainer cont = new ExperimentIndicatorContainer();

                            cont.ExperimentIndicatorID = reader.GetInt32(0);
                            cont.ExperimentID = reader.GetInt32(1);
                            cont.ExcitationFilterDesc = reader.GetString(2);
                            cont.EmissionFilterDesc = reader.GetString(3);
                            cont.ExcitationFilterPos = reader.GetInt32(4);
                            cont.EmissionFilterPos = reader.GetInt32(5);
                            cont.MaskID = reader.GetInt32(6);
                            cont.Exposure = reader.GetInt32(7);
                            cont.Gain = reader.GetInt32(8);
                            cont.Description = reader.GetString(9);
                            cont.SignalType = (SIGNAL_TYPE)reader.GetInt32(10);
                            cont.FlatFieldCorrection = (FLATFIELD_SELECT)reader.GetInt32(11);

                            m_expIndicatorList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAllExperimentIndicatorsForExperiment(int experimentID, out ObservableCollection<ExperimentIndicatorContainer> expIndicatorList)
        {
            bool success = true;

            expIndicatorList = new ObservableCollection<ExperimentIndicatorContainer>();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentIndicator WHERE ExperimentID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = experimentID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            ExperimentIndicatorContainer cont = new ExperimentIndicatorContainer();

                            cont.ExperimentIndicatorID = reader.GetInt32(0);
                            cont.ExperimentID = reader.GetInt32(1);
                            cont.ExcitationFilterDesc = reader.GetString(2);
                            cont.EmissionFilterDesc = reader.GetString(3);
                            cont.ExcitationFilterPos = reader.GetInt32(4);
                            cont.EmissionFilterPos = reader.GetInt32(5);
                            cont.MaskID = reader.GetInt32(6);
                            cont.Exposure = reader.GetInt32(7);
                            cont.Gain = reader.GetInt32(8);
                            cont.Description = reader.GetString(9);
                            cont.SignalType = (SIGNAL_TYPE)reader.GetInt32(10);
                            cont.FlatFieldCorrection = (FLATFIELD_SELECT)reader.GetInt32(11);

                            expIndicatorList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetExperimentIndicator(int expIndicatorID, out ExperimentIndicatorContainer indicator)
        {
            bool success = true;

            indicator = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM ExperimentIndicator WHERE ExperimentIndicatorID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = expIndicatorID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            indicator = new ExperimentIndicatorContainer();

                            indicator.ExperimentIndicatorID = reader.GetInt32(0);
                            indicator.ExperimentID = reader.GetInt32(1);
                            indicator.ExcitationFilterDesc = reader.GetString(2);
                            indicator.EmissionFilterDesc = reader.GetString(3);
                            indicator.ExcitationFilterPos = reader.GetInt32(4);
                            indicator.EmissionFilterPos = reader.GetInt32(5);
                            indicator.MaskID = reader.GetInt32(6);
                            indicator.Exposure = reader.GetInt32(7);
                            indicator.Gain = reader.GetInt32(8);
                            indicator.Description = reader.GetString(9);
                            indicator.SignalType = (SIGNAL_TYPE)reader.GetInt32(10);
                            indicator.FlatFieldCorrection = (FLATFIELD_SELECT)reader.GetInt32(11);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertExperimentIndicator(ref ExperimentIndicatorContainer indicator)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO ExperimentIndicator (ExperimentID,ExcitationFilterDesc,EmissionFilterDesc,ExcitationFilterPos,EmissionFilterPos,MaskID,Exposure,Gain,Description,SignalType,FlatFieldCorrection) "
                                                            + "OUTPUT INSERTED.ExperimentIndicatorID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", indicator.ExperimentID);
                        command.Parameters.AddWithValue("@p2", indicator.ExcitationFilterDesc);
                        command.Parameters.AddWithValue("@p3", indicator.EmissionFilterDesc);
                        command.Parameters.AddWithValue("@p4", indicator.ExcitationFilterPos);
                        command.Parameters.AddWithValue("@p5", indicator.EmissionFilterPos);
                        command.Parameters.AddWithValue("@p6", indicator.MaskID);
                        command.Parameters.AddWithValue("@p7", indicator.Exposure);
                        command.Parameters.AddWithValue("@p8", indicator.Gain);
                        command.Parameters.AddWithValue("@p9", indicator.Description);
                        command.Parameters.AddWithValue("@p10", (int)indicator.SignalType);
                        command.Parameters.AddWithValue("@p11", (int)indicator.FlatFieldCorrection);

                        indicator.ExperimentIndicatorID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateExperimentIndicator(ExperimentIndicatorContainer indicator)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE ExperimentIndicator SET ExperimentID=@p1,ExcitationFilterDesc=@p2,EmissionFilterDesc=@p3,ExcitationFilterPos=@p4," +
                                                           "EmissionFilterPos=@p5,MaskID=@p6,Exposure=@p7,Gain=@p8,Description=@p9,SignalType=@p10,FlatFieldCorrection=@p11 " +
                                                            "WHERE ExperimentIndicatorID=@p12", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", indicator.ExperimentID);
                        command.Parameters.AddWithValue("@p2", indicator.ExcitationFilterDesc);
                        command.Parameters.AddWithValue("@p3", indicator.EmissionFilterDesc);
                        command.Parameters.AddWithValue("@p4", indicator.ExcitationFilterPos);
                        command.Parameters.AddWithValue("@p5", indicator.EmissionFilterPos);
                        command.Parameters.AddWithValue("@p6", indicator.MaskID);
                        command.Parameters.AddWithValue("@p7", indicator.Exposure);
                        command.Parameters.AddWithValue("@p8", indicator.Gain);
                        command.Parameters.AddWithValue("@p9", indicator.Description);
                        command.Parameters.AddWithValue("@p10", (int)indicator.SignalType);
                        command.Parameters.AddWithValue("@p11", (int)indicator.FlatFieldCorrection);
                        command.Parameters.AddWithValue("@p12", indicator.ExperimentIndicatorID);                        

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteExperimentIndicator(int expIndicatorID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM ExperimentIndicator WHERE ExperimentIndicatorID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", expIndicatorID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }




        #endregion


        #region EventMarker
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // EventMarker



        public bool GetAllEventMarkersForExperiment(int experimentID, out List<EventMarkerContainer> eventMarkerList)
        {
            bool success = true;

            eventMarkerList = new List<EventMarkerContainer>();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM EventMarker WHERE ExperimentID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = experimentID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            EventMarkerContainer cont = new EventMarkerContainer();

                            cont.EventMarkerID = reader.GetInt32(0);
                            cont.ExperimentID = reader.GetInt32(1);
                            cont.SequenceNumber = reader.GetInt32(2);
                            cont.Name = reader.GetString(3);
                            cont.Description = reader.GetString(4);
                            cont.TimeStamp = reader.GetDateTime(5);

                            eventMarkerList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetEventMarker(int eventMarkerID, out EventMarkerContainer eventMarker)
        {
            bool success = true;

            eventMarker = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM EventMarker WHERE EventMarkerID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = eventMarkerID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            eventMarker = new EventMarkerContainer();

                            eventMarker.EventMarkerID = reader.GetInt32(0);
                            eventMarker.ExperimentID = reader.GetInt32(1);
                            eventMarker.SequenceNumber = reader.GetInt32(2);
                            eventMarker.Name = reader.GetString(3);
                            eventMarker.Description = reader.GetString(4);
                            eventMarker.TimeStamp = reader.GetDateTime(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertEventMarker(ref EventMarkerContainer eventMarker)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO EventMarker (ExperimentID,SequenceNumber,Name,Description,TimeStamp) "
                                                            + "OUTPUT INSERTED.EventMarkerID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", eventMarker.ExperimentID);
                        command.Parameters.AddWithValue("@p2", eventMarker.SequenceNumber);
                        command.Parameters.AddWithValue("@p3", eventMarker.Name);
                        command.Parameters.AddWithValue("@p4", eventMarker.Description);

                        SqlParameter DateTimeParam = new SqlParameter("@p5", SqlDbType.DateTime2);
                        DateTimeParam.Value = eventMarker.TimeStamp;
                        command.Parameters.Add(DateTimeParam);
                        
                        eventMarker.EventMarkerID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool UpdateEventMarker(EventMarkerContainer eventMarker)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE EventMarker SET ExperimentID=@p1,SequenceNumber=@p2,Name=@p3,Description=@p4,TimeStamp=@p5 " +
                                                            "WHERE EventMarkerID=@p6", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", eventMarker.ExperimentID);
                        command.Parameters.AddWithValue("@p2", eventMarker.SequenceNumber);
                        command.Parameters.AddWithValue("@p3", eventMarker.Name);
                        command.Parameters.AddWithValue("@p4", eventMarker.Description);

                        SqlParameter DateTimeParam = new SqlParameter("@p5", SqlDbType.DateTime2);
                        DateTimeParam.Value = eventMarker.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p6", eventMarker.EventMarkerID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteEventMarker(int eventMarkerID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM EventMarker WHERE EventMarkerID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", eventMarkerID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool DeleteEventMarkersForExperiment(int experimentID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM EventMarker WHERE ExperimentID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", experimentID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        #endregion



        #region Analysis
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Analysis



        public bool GetAllAnalysesForExperimentIndicator(int expIndicatorID)
        {

            // this does not get the Analysis Values for each Analysis

            bool success = true;

            m_analysisList.Clear();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Analysis WHERE ExperimentIndicatorID=@p1 ORDER BY TimeStamp ASC", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = expIndicatorID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            AnalysisContainer cont = new AnalysisContainer();

                            cont.AnalysisID = reader.GetInt32(0);
                            cont.ExperimentIndicatorID = reader.GetInt32(1);                           
                            cont.Description = reader.GetString(2);
                            cont.TimeStamp = reader.GetDateTime(3);
                            cont.RuntimeAnalysis = reader.GetBoolean(4);
                            cont.FlatFieldRefImageID = reader.GetInt32(5);
                            cont.DarkRefImageID = reader.GetInt32(6);

                            m_analysisList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAnalysis(int analysisID, out AnalysisContainer analysis)
        {
            bool success = true;

            analysis = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Analysis WHERE AnalysisID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = analysisID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            analysis = new AnalysisContainer();

                            analysis.AnalysisID = reader.GetInt32(0);
                            analysis.ExperimentIndicatorID = reader.GetInt32(1);
                            analysis.Description = reader.GetString(2);
                            analysis.TimeStamp = reader.GetDateTime(3);
                            analysis.RuntimeAnalysis = reader.GetBoolean(4);
                            analysis.FlatFieldRefImageID = reader.GetInt32(5);
                            analysis.DarkRefImageID = reader.GetInt32(6);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }


                // go get the analysis values for this analysis,  values are stored in m_analysisFrameList
                success = GetAllAnalysisFramesForAnalysis(analysisID);

            }

            return success;
        }



        public bool InsertAnalysis(ref AnalysisContainer analysis)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO Analysis (ExperimentIndicatorID,Description,TimeStamp,RuntimeAnalysis,FlatFieldRefImageID,DarkRefImageID) "
                                                            + "OUTPUT INSERTED.AnalysisID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysis.ExperimentIndicatorID);                        
                        command.Parameters.AddWithValue("@p2", analysis.Description);

                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = analysis.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p4", analysis.RuntimeAnalysis);

                        command.Parameters.AddWithValue("@p5", analysis.FlatFieldRefImageID);
                        command.Parameters.AddWithValue("@p6", analysis.DarkRefImageID);

                        analysis.AnalysisID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                    
                }
            }

            return success;
        }



        public bool UpdateAnalysis(AnalysisContainer analysis)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Analysis SET ExperimentIndicatorID=@p1,Description=@p2,TimeStamp=@p3,RuntimeAnalysis=@p4," +
                                                           "FlatFieldRefImageID=@p5,DarkRefImageID=@p6 " +
                                                            "WHERE AnalysisID=@p7", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysis.ExperimentIndicatorID);
                        command.Parameters.AddWithValue("@p2", analysis.Description);
                        
                        SqlParameter DateTimeParam = new SqlParameter("@p3", SqlDbType.DateTime2);
                        DateTimeParam.Value = analysis.TimeStamp;
                        command.Parameters.Add(DateTimeParam);

                        command.Parameters.AddWithValue("@p4", analysis.RuntimeAnalysis);

                        command.Parameters.AddWithValue("@p5", analysis.FlatFieldRefImageID);
                        command.Parameters.AddWithValue("@p6", analysis.DarkRefImageID);

                        command.Parameters.AddWithValue("@p7", analysis.AnalysisID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteAnalysis(int analysisID)
        {
            bool success = true;

            success = DeleteAllAnalysisFramesForAnalysis(analysisID);

            if(success)
                using (SqlConnection con = new SqlConnection(m_connectionString))
                {
                    con.Open();

                    using (SqlCommand command = new SqlCommand("DELETE FROM Analysis WHERE AnalysisID=@p1", con))
                    {
                        try
                        {
                            command.Parameters.AddWithValue("@p1", analysisID);

                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            lastErrMsg = e.Message;
                            success = false;
                            RecordError(e.Message);
                        }
                    }
                }

            return success;
        }


    
        #endregion



        #region Analysis Frame
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Analysis Frame



        public bool GetAllAnalysisFramesForAnalysis(int analysisID)
        {
            bool success = true;

            m_analysisFrameList.Clear();

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM AnalysisFrame WHERE AnalysisID=@p1 ORDER BY SequenceNumber ASC", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = analysisID;

                    command.CommandTimeout = 60;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            AnalysisFrameContainer cont = new AnalysisFrameContainer();

                            cont.AnalysisFrameID = reader.GetInt32(0);
                            cont.AnalysisID = reader.GetInt32(1);
                            cont.SequenceNumber = reader.GetInt32(2);
                            cont.Rows = reader.GetInt32(3);
                            cont.Cols = reader.GetInt32(4);                            
                            cont.ValueString = reader.GetString(5);

                            m_analysisFrameList.Add(cont);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool GetAnalysisFrame(int analysisFrameID, out AnalysisFrameContainer analysisFrame)
        {
            bool success = true;

            analysisFrame = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM AnalysisFrame WHERE AnalysisFrameID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = analysisFrameID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            analysisFrame = new AnalysisFrameContainer();

                            analysisFrame.AnalysisFrameID = reader.GetInt32(0);
                            analysisFrame.AnalysisID = reader.GetInt32(1);
                            analysisFrame.SequenceNumber = reader.GetInt32(2);
                            analysisFrame.Rows = reader.GetInt32(3);
                            analysisFrame.Cols = reader.GetInt32(4);                            
                            analysisFrame.ValueString = reader.GetString(5);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertAnalysisFrame(ref AnalysisFrameContainer analysisFrame)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO AnalysisFrame (AnalysisID,SequenceNumber,Rows,Cols,ValueString) "
                                                            + "OUTPUT INSERTED.AnalysisFrameID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysisFrame.AnalysisID);
                        command.Parameters.AddWithValue("@p2", analysisFrame.SequenceNumber);
                        command.Parameters.AddWithValue("@p3", analysisFrame.Rows);
                        command.Parameters.AddWithValue("@p4", analysisFrame.Cols);                        
                        command.Parameters.AddWithValue("@p5", analysisFrame.ValueString);

                        analysisFrame.AnalysisFrameID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool InsertAnalysisFrame(int analysisID, int sequenceNumber, float[,] data)
        {
            bool success = true;

            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            // build ValueString
            StringBuilder builder = new StringBuilder();            
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    builder.AppendFormat("{0:G}",data[r, c]/7).Append(',');
                }
            // remove last comma
            builder.Remove(builder.Length - 1, 1);

            // build AnalysisFrameContainer
            AnalysisFrameContainer analysisFrame = new AnalysisFrameContainer();
            analysisFrame.AnalysisID = analysisID;
            analysisFrame.SequenceNumber = sequenceNumber;
            analysisFrame.Rows = rows;
            analysisFrame.Cols = cols;
            analysisFrame.ValueString = builder.ToString();

            success = InsertAnalysisFrame(ref analysisFrame);

            return success;
        }



        public bool UpdateAnalysisFrame(AnalysisFrameContainer analysisFrame)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE AnalysisFrame SET AnalysisID=@p1,SequenceNumber=@p2,Rows=@p3,Cols=@p4,ValueString=@p5 " +
                                                            "WHERE AnalysisFrameID=@p6", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysisFrame.AnalysisID);
                        command.Parameters.AddWithValue("@p2", analysisFrame.SequenceNumber);
                        command.Parameters.AddWithValue("@p3", analysisFrame.Rows);
                        command.Parameters.AddWithValue("@p4", analysisFrame.Cols);                        
                        command.Parameters.AddWithValue("@p5", analysisFrame.ValueString);

                        command.Parameters.AddWithValue("@p6", analysisFrame.AnalysisFrameID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }



        public bool DeleteAnalysisFrame(int analysisFrameID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM AnalysisFrame WHERE AnalysisFrameID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysisFrameID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool DeleteAllAnalysisFramesForAnalysis(int analysisID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM AnalysisFrame WHERE AnalysisID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", analysisID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        #endregion



        #region Image Compression
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Image Compression

        public bool DecompressImage(COMPRESSION_ALGORITHM algorithm, byte[] compressedImage, out ushort[] uncompressedImage)
        {
            bool success = true;
            switch (algorithm)
            {
                case COMPRESSION_ALGORITHM.NONE:
                    uncompressedImage = new ushort[compressedImage.Length / sizeof(ushort)];
                    Buffer.BlockCopy(compressedImage, 0, uncompressedImage, 0, compressedImage.Length);
                    break;
                case COMPRESSION_ALGORITHM.GZIP:
                    uncompressedImage = Zip.Decompress_ByteToShort_Simple(compressedImage);
                    break;
                default:
                    uncompressedImage = null;
                    success = false;
                    RecordError("Failure Decompressing Image");
                    break;
            }

            return success;
        }


        public bool CompressImage(COMPRESSION_ALGORITHM algorithm, ushort[] uncompressedImage, out byte[] compressedImage)
        {
            bool success = true;

            switch (algorithm)
            {
                case COMPRESSION_ALGORITHM.NONE:
                    compressedImage = new byte[uncompressedImage.Length * sizeof(ushort)];
                    Buffer.BlockCopy(uncompressedImage, 0, compressedImage, 0, uncompressedImage.Length * sizeof(ushort));
                    break;
                case COMPRESSION_ALGORITHM.GZIP:
                    compressedImage = Zip.Compress_ShortToByte_Simple(uncompressedImage);
                    break;
                default:
                    compressedImage = null;
                    success = false;
                    RecordError("Failure Compressing Image");
                    break;
            }

            return success;
        }


        public bool DecompressMask(COMPRESSION_ALGORITHM algorithm, byte[] compressedImage, out byte[] uncompressedImage)
        {
            bool success = true;
            switch (algorithm)
            {
                case COMPRESSION_ALGORITHM.NONE:
                    uncompressedImage = new byte[compressedImage.Length];
                    Buffer.BlockCopy(compressedImage, 0, uncompressedImage, 0, compressedImage.Length);
                    break;
                case COMPRESSION_ALGORITHM.GZIP:                    
                    uncompressedImage = Zip.Decompress_ByteToByte_Simple(compressedImage);
                    break;
                default:
                    uncompressedImage = null;
                    success = false;
                    RecordError("Failure Decompressing Image");
                    break;
            }

            return success;
        }


        public bool CompressMask(COMPRESSION_ALGORITHM algorithm, byte[] uncompressedImage, out byte[] compressedImage)
        {
            bool success = true;

            switch (algorithm)
            {
                case COMPRESSION_ALGORITHM.NONE:
                    compressedImage = new byte[uncompressedImage.Length];
                    Buffer.BlockCopy(uncompressedImage, 0, compressedImage, 0, uncompressedImage.Length);
                    break;
                case COMPRESSION_ALGORITHM.GZIP:
                    compressedImage = Zip.Compress_ByteToByte_Simple(uncompressedImage);
                    break;
                default:
                    compressedImage = null;
                    success = false;
                    RecordError("Failure Compressing Image");
                    break;
            }

            return success;
        }




        #endregion



        #region Camera Settings
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        // Camera Settings

        public bool GetAllCameraSettings()
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM CameraSettings", con))
                {
                    m_cameraSettingsList.Clear();

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            CameraSettingsContainer cameraSettings = new CameraSettingsContainer();

                            cameraSettings.CameraSettingID = reader.GetInt32(0);
                            cameraSettings.VSSIndex = reader.GetInt32(1);
                            cameraSettings.HSSIndex = reader.GetInt32(2);
                            cameraSettings.VertClockAmpIndex = reader.GetInt32(3);
                            cameraSettings.UseEMAmp = reader.GetBoolean(4);
                            cameraSettings.UseFrameTransfer = reader.GetBoolean(5);   
                            cameraSettings.Description = reader.GetString(6);
                            cameraSettings.IsDefault = reader.GetBoolean(7);
                            cameraSettings.StartingExposure = reader.GetInt32(8);
                            cameraSettings.ExposureLimit = reader.GetInt32(9);
                            cameraSettings.HighPixelThresholdPercent = reader.GetInt32(10);
                            cameraSettings.LowPixelThresholdPercent = reader.GetInt32(11);
                            cameraSettings.MinPercentPixelsAboveLowThreshold = reader.GetInt32(12);
                            cameraSettings.MaxPercentPixelsAboveHighThreshold = reader.GetInt32(13);
                            cameraSettings.IncreasingSignal = reader.GetBoolean(14);
                            cameraSettings.StartingBinning = reader.GetInt32(15);
                            cameraSettings.EMGainLimit = reader.GetInt32(16);

                            m_cameraSettingsList.Add(cameraSettings);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool GetCameraSettingsDefault(out CameraSettingsContainer cameraSettings)
        {
            // return cameraSettings = null if there is no default camera settings

            bool success = true;

            cameraSettings = null;

            if(GetAllCameraSettings())
            {
                foreach(CameraSettingsContainer cont in m_cameraSettingsList)
                {
                    if(cont.IsDefault)
                    {
                        cameraSettings = cont;
                        break;
                    }
                }
            }

            return success;
        }


        public bool GetCameraSettings(int cameraSettingsID, out CameraSettingsContainer cameraSettings)
        {
            bool success = true;

            cameraSettings = null;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM CameraSettings WHERE CameraSettingsID=@p1", con))
                {
                    command.Parameters.Add("@p1", System.Data.SqlDbType.Int).Value = cameraSettingsID;

                    try
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            cameraSettings = new CameraSettingsContainer();
                                                  
                            cameraSettings.CameraSettingID = reader.GetInt32(0);
                            cameraSettings.VSSIndex = reader.GetInt32(1);
                            cameraSettings.HSSIndex = reader.GetInt32(2);
                            cameraSettings.VertClockAmpIndex = reader.GetInt32(3);
                            cameraSettings.UseEMAmp = reader.GetBoolean(4);
                            cameraSettings.UseFrameTransfer = reader.GetBoolean(5);
                            cameraSettings.Description = reader.GetString(6);
                            cameraSettings.IsDefault = reader.GetBoolean(7);
                            cameraSettings.StartingExposure = reader.GetInt32(8);
                            cameraSettings.ExposureLimit = reader.GetInt32(9);
                            cameraSettings.HighPixelThresholdPercent = reader.GetInt32(10);
                            cameraSettings.LowPixelThresholdPercent = reader.GetInt32(11);
                            cameraSettings.MinPercentPixelsAboveLowThreshold = reader.GetInt32(12);
                            cameraSettings.MaxPercentPixelsAboveHighThreshold = reader.GetInt32(13);
                            cameraSettings.IncreasingSignal = reader.GetBoolean(14);
                            cameraSettings.StartingBinning = reader.GetInt32(15);
                            cameraSettings.EMGainLimit = reader.GetInt32(16);
                        }
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool InsertCameraSettings(ref CameraSettingsContainer cameraSettings)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("INSERT INTO CameraSettings (VSSIndex,HSSIndex,VertClockAmpIndex,UseEMAmp,UseFrameTransfer,Description,IsDefault,StartingExposure," 
                                                            + "ExposureLimit,HighPixelThresholdPercent,LowPixelThresholdPercent,MinPercentPixelsAboveLowThreshold,"
                                                            + "MaxPercentPixelsAboveHighThreshold,IncreasingSignal,StartingBinning,EMGainLimit) "
                                                            + "OUTPUT INSERTED.CameraSettingsID "
                                                            + "VALUES(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16)"
                                                            , con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", cameraSettings.VSSIndex);
                        command.Parameters.AddWithValue("@p2", cameraSettings.HSSIndex);
                        command.Parameters.AddWithValue("@p3", cameraSettings.VertClockAmpIndex);
                        command.Parameters.AddWithValue("@p4", cameraSettings.UseEMAmp);
                        command.Parameters.AddWithValue("@p5", cameraSettings.UseFrameTransfer);
                        command.Parameters.AddWithValue("@p6", cameraSettings.Description);
                        command.Parameters.AddWithValue("@p7", cameraSettings.IsDefault);
                        command.Parameters.AddWithValue("@p8", cameraSettings.StartingExposure);
                        command.Parameters.AddWithValue("@p9", cameraSettings.ExposureLimit);
                        command.Parameters.AddWithValue("@p10", cameraSettings.HighPixelThresholdPercent);
                        command.Parameters.AddWithValue("@p11", cameraSettings.LowPixelThresholdPercent);
                        command.Parameters.AddWithValue("@p12", cameraSettings.MinPercentPixelsAboveLowThreshold);
                        command.Parameters.AddWithValue("@p13", cameraSettings.MaxPercentPixelsAboveHighThreshold);
                        command.Parameters.AddWithValue("@p14", cameraSettings.IncreasingSignal);
                        command.Parameters.AddWithValue("@p15", cameraSettings.StartingBinning);
                        command.Parameters.AddWithValue("@p16", cameraSettings.EMGainLimit);

                        cameraSettings.CameraSettingID = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool UpdateCameraSettings(CameraSettingsContainer cameraSettings)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("UPDATE CameraSettings SET VSSIndex=@p1,HSSIndex=@p2,VertClockAmpIndex=@p3," +
                                                           "UseEMAmp=@p4,UseFrameTransfer=@p5,Description=@p6,IsDefault=@p7,StartingExposure=@p8,ExposureLimit=@p9," +
                                                           "HighPixelThresholdPercent=@p10,LowPixelThresholdPercent=@p11,MinPercentPixelsAboveLowThreshold=@p12," +
                                                           "MaxPercentPixelsAboveHighThreshold=@p13,IncreasingSignal=@p14," +
                                                           "StartingBinning=@p15,EMGainLimit=@p16 " +
                                                           "WHERE CameraSettingsID=@p17", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", cameraSettings.VSSIndex);
                        command.Parameters.AddWithValue("@p2", cameraSettings.HSSIndex);
                        command.Parameters.AddWithValue("@p3", cameraSettings.VertClockAmpIndex);
                        command.Parameters.AddWithValue("@p4", cameraSettings.UseEMAmp);
                        command.Parameters.AddWithValue("@p5", cameraSettings.UseFrameTransfer);
                        command.Parameters.AddWithValue("@p6", cameraSettings.Description);
                        command.Parameters.AddWithValue("@p7", cameraSettings.IsDefault);
                        command.Parameters.AddWithValue("@p8", cameraSettings.StartingExposure);
                        command.Parameters.AddWithValue("@p9", cameraSettings.ExposureLimit);
                        command.Parameters.AddWithValue("@p10", cameraSettings.HighPixelThresholdPercent);
                        command.Parameters.AddWithValue("@p11", cameraSettings.LowPixelThresholdPercent);
                        command.Parameters.AddWithValue("@p12", cameraSettings.MinPercentPixelsAboveLowThreshold);
                        command.Parameters.AddWithValue("@p13", cameraSettings.MaxPercentPixelsAboveHighThreshold);
                        command.Parameters.AddWithValue("@p14", cameraSettings.IncreasingSignal);
                        command.Parameters.AddWithValue("@p15", cameraSettings.StartingBinning);
                        command.Parameters.AddWithValue("@p16", cameraSettings.EMGainLimit);

                        command.Parameters.AddWithValue("@p17", cameraSettings.CameraSettingID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }


        public bool DeleteCameraSettings(int cameraSettingsID)
        {
            bool success = true;

            using (SqlConnection con = new SqlConnection(m_connectionString))
            {
                con.Open();

                using (SqlCommand command = new SqlCommand("DELETE FROM CameraSettings WHERE CameraSettingsID=@p1", con))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@p1", cameraSettingsID);

                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        lastErrMsg = e.Message;
                        success = false;
                        RecordError(e.Message);
                    }
                }
            }

            return success;
        }
        
        #endregion

    }
}
