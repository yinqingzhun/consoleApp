//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace EFContext
{
    using System;
    using System.Collections.Generic;
    
    public partial class AOH_Calllog
    {
        public int CalllogID { get; set; }
        public int ForeignId { get; set; }
        public string CallerPhoneNumber { get; set; }
        public string CalleePhoneNumber { get; set; }
        public string CalleeExtenNumber { get; set; }
        public string CalleeRealNumber { get; set; }
        public System.DateTime CallBeginTime { get; set; }
        public System.DateTime CallFinishTime { get; set; }
        public int CallerDuration { get; set; }
        public int CalleeDuration { get; set; }
        public string RecordingFileUrl { get; set; }
        public int CallerLocationID { get; set; }
        public string CallerLocationName { get; set; }
    }
}
