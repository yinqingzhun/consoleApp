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
    
    public partial class aoh_club_recommend_topic
    {
        public long id { get; set; }
        public System.DateTime create_time { get; set; }
        public System.DateTime update_time { get; set; }
        public long club_id { get; set; }
        public string club_name { get; set; }
        public string cover_url { get; set; }
        public int display_no { get; set; }
        public System.DateTime publish_time { get; set; }
        public System.DateTime revoke_time { get; set; }
        public string title { get; set; }
        public long topic_id { get; set; }
        public string type { get; set; }
        public string update_user { get; set; }
    }
}
