﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace achihapi.ViewModels
{
    public class LearnObjectViewModel : BaseViewModel
    {
        public Int32 ID { get; set; }
        [Required]
        public Int32 HID { get; set; }
        public Int32 CategoryID { get; set; }
        [Required]
        [StringLength(45)]
        public String Name { get; set; }
        [Required]
        public String Content { get; set; }
    }

    public class LearnObjectUIViewModel : LearnObjectViewModel
    {
        // UI part string
        public String CategoryName { get; set; }
    }
}
