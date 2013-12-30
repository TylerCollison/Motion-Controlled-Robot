    using Microsoft.Ccr.Core;
    using Microsoft.Dss.Core.Attributes;
    using Microsoft.Dss.ServiceModel.Dssp;
    using System;
    using System.Collections.Generic;
    using W3C.Soap;
    using legotribot = Robotics.MyTutorial1;

namespace Robotics.MyTutorial1
{

        /// <summary>
        /// LegoTriBot Contract class
        /// </summary>
        public sealed class Contract
        {
            /// <summary>
            /// The Dss Service contract
            /// </summary>
            public const String Identifier = "http://schemas.tempuri.org/2013/11/mytutorial1.html";
        }
        /// <summary>
        /// The LegoTriBot State
        /// </summary>
        [DataContract()]
        public class MyTutorial1State
        {
            // maintain whether the motors are enabled
            public bool MotorEnabled;
        }
        /// <summary>
        /// LegoTriBot Main Operations Port
        /// </summary>
        [ServicePort()]
        public class MyTutorial1Operations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
        {
        }
        /// <summary>
        /// LegoTriBot Get Operation
        /// </summary>
        public class Get : Get<GetRequestType, PortSet<MyTutorial1State, Fault>>
        {
            /// <summary>
            /// LegoTriBot Get Operation
            /// </summary>
            public Get()
            {
            }
            /// <summary>
            /// LegoTriBot Get Operation
            /// </summary>
            public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body) :
                base(body)
            {
            }
            /// <summary>
            /// LegoTriBot Get Operation
            /// </summary>
            public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body, Microsoft.Ccr.Core.PortSet<MyTutorial1State, W3C.Soap.Fault> responsePort) :
                base(body, responsePort)
            {
            }
        }
}


