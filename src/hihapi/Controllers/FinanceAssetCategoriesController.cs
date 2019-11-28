using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNet.OData;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using hihapi.Models;

namespace hihapi.Controllers
{
    public class FinanceAssetCategoriesController: ODataController
    {        
        private readonly hihDataContext _context;
        
        public FinanceAssetCategoriesController(hihDataContext context)
        {
            _context = context;
        }
        
        /// GET: /FinanceAssertCategories
        [EnableQuery]
        [Authorize]
        public IQueryable<FinanceAssetCategory> Get()
        {
            return _context.FinAssetCategories;
        }
    }
}