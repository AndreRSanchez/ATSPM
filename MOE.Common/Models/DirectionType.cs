﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;
using System.Runtime.Serialization;

namespace MOE.Common.Models
{
    [DataContract]
    public partial class DirectionType
    {
        public DirectionType()
        {

        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [DataMember]
        public int DirectionTypeID { get; set; }

        [StringLength(30)]
        [DataMember]
        public string Description { get; set; }

        [StringLength(5)]
        [DataMember]
        public string Abbreviation { get; set; }

        [DataMember]
        public int DisplayOrder { get; set; }
        
    }
}
