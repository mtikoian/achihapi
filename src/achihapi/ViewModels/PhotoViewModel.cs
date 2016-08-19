﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace achihapi.ViewModels
{
    public class PhotoViewModelBase
    {
        [Required]
        [StringLength(40)]
        public String PhotoId { get; set; }
        [StringLength(50)]
        public String Title { get; set; }
        [StringLength(100)]
        public String Desp { get; set; }
        [StringLength(100)]
        public String FileUrl { get; set; }
        [StringLength(100)]
        public String ThumbnailFileUrl { get; set; }
        public String FileFormat { get; set; }
        public DateTime UploadedTime { get; set; }
        [StringLength(100)]
        public String OrgFileName { get; set; }
        public Boolean IsOrgThumbnail { get; set; }
    }

    public class PhotoViewModel : PhotoViewModelBase
    {
        public List<ExifTagItem> ExifTags = new List<ExifTagItem>();
    }
}
