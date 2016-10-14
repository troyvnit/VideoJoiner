using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VideoJoiner.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Setting")]
    public partial class Setting
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string SettingKey { get; set; }

        [StringLength(500)]
        public string SettingValue { get; set; }

        [StringLength(50)]
        public string SettingLabel { get; set; }

        [StringLength(50)]
        public string SettingPlaceholder { get; set; }
    }
}
