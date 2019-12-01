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
using hihapi.Utilities;

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
        public IQueryable<FinanceAssetCategory> Get(Int32? hid = null)
        {
            if (hid.HasValue)
            {
                String usrName = String.Empty;
                try
                {
                    usrName = HIHAPIUtility.GetUserID(this);
                }
                catch
                {
                    // Do nothing
                }

                var rst = 
                    from hmem in _context.HomeMembers.Where(p => p.User == usrName && p.HomeID == hid.Value)
                    from ctgy in _context.FinAssetCategories.Where(p => p.HomeID == null || p.HomeID == hmem.HomeID)
                    select ctgy;

                return rst;
            }
            
            return _context.FinAssetCategories.Where(p => p.HomeID == null);
        }
    }
}
