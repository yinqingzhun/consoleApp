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
    
    public partial class AOH_ActivityCoordinatePoint
    {
        public int PointID { get; set; }
        public int TeamID { get; set; }
        public Nullable<decimal> Longitude { get; set; }
        public Nullable<decimal> Latitude { get; set; }
        public string Description { get; set; }
        public System.DateTime CreateTime { get; set; }
        public int CreatorID { get; set; }
        public string CreatorName { get; set; }
        public bool Enable { get; set; }
    }
}