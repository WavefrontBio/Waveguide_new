using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Waveguide
{
    namespace WaveGuideEvents {

        public class StringMessageEventArgs : EventArgs
        {
            private string _message;

            public StringMessageEventArgs(string msg)
            {
                _message = msg;
            }

            public string Message
            {
                get { return this._message; }
                set { this._message = value; }
            }
        }

        // custom EventArgs for PostError event
        public class ErrorEventArgs : EventArgs
        {
            private string _errMsg;

            public ErrorEventArgs(string errMsg)
            {
                _errMsg = errMsg;
            }

            public string ErrorMessage
            {
                get { return this._errMsg; }
                set { this._errMsg = value; }
            }
        }



        public class StatusEvent
        {
            public StatusEvent(String message)
            {
                Message = message;
            }

            public String Message { get; private set; }
        }

        public class ErrorMessageEvent
        {
            public ErrorMessageEvent(string message)
            {
                Message = message;
            }

            public string Message { get; private set; }
        }

 
        public class StartBravoEvent
        {
            private string bravoMethodFileName;
            private int plateID;
            private int labelSetIndex;
            private int cycleTimeIndex;

            public StartBravoEvent(string filename, int ID, int label, int cycletime)
            {
                bravoMethodFileName = filename;
                plateID = ID;
                labelSetIndex = label;
                cycleTimeIndex = cycletime;
            }

            public string BravoFile
            {
                get { return bravoMethodFileName; }
            }

            public int PlateID { get { return plateID; } }


            public int CycleTimeIndex { get { return cycleTimeIndex; } }


            public int LabelSetIndex { get { return labelSetIndex; } }

        }



        public class VWorksCommandEventArgs : EventArgs
        {
            private VWORKS_COMMAND _command;
            private int _param1;
            private string _name;
            private string _description;


            public VWORKS_COMMAND Command
            {
                get { return this._command; }
                set { this._command = value; }
            }

            public int Param1
            {
                get { return this._param1; }
                set { this._param1 = value; }
            }

            public string Name
            {
                get { return this._name; }
                set { this._name = value; }
            }

            public string Description
            {
                get { return this._description; }
                set { this._description = value; }
            }


            public VWorksCommandEventArgs(VWORKS_COMMAND command, int param1 = 0, string name = "", string description = "")
            {
                _command = command;
                _param1 = param1;
                _name = name;
                _description = description;
            }
        }


        public class CameraTemperatureEventArgs : EventArgs
        {
            private readonly int _temperature;

            public CameraTemperatureEventArgs(int temp)
            {
                _temperature = temp;
            }

            public int Temperature
            {
                get { return this._temperature; }                
            }
        }



    }
}
