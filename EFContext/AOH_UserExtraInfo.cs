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
    
    public partial class AOH_UserExtraInfo
    {
        public int UserExtraInfoID { get; set; }
        public int UserID { get; set; }
        public System.DateTime FirstLoginTime { get; set; }
        public System.DateTime LastLoginTime { get; set; }
        public string UserName { get; set; }
        public Nullable<System.DateTime> LastMessageQueryTime { get; set; }
        public string ImID { get; set; }
        public string ImPassword { get; set; }
        public Nullable<System.DateTime> LastLogoutTime { get; set; }
    }
}
