﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MOE.Common.Models
{
    [DataContract]
    public partial class Approach
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Approach()
        {
            //LaneGroups = new HashSet<LaneGroup>();
        }

        [Key]
        [DataMember]
        public int ApproachID { get; set; }

        [DataMember]
        public string SignalID { get; set; }

        [Required]
        [DataMember]
        public int VersionID { get; set; }

        [DataMember]
        public virtual Signal Signal { get; set; }

        [Required]
        [Display(Name="Direction")]
        [DataMember]
        public int DirectionTypeID { get; set; }

        [DataMember]
        public DirectionType DirectionType { get; set; }

        [DataMember]
        public string Description { get; set; }

        [Display(Name = "MPH (Advanced Count, Advanced Speed)")]
        [DataMember]
        public int? MPH { get; set; }

        [Required]
        [Display(Name = "Protected Phase")]
        [DataMember]
        public int ProtectedPhaseNumber { get; set; }

        [Display(Name = "Protected Phase Overlap")]
        [DataMember]
        public bool IsProtectedPhaseOverlap { get; set; }

        [Display(Name = "Permissive Phase")]
        [DataMember]
        public int? PermissivePhaseNumber { get; set; }

        [Display(Name = "Perm. Phase Overlap")]
        [DataMember]
        public bool IsPermissivePhaseOverlap { get; set; }

        [DataMember]
        public virtual ICollection<Detector> Detectors { get; set; }

     

    }
}
