﻿using System;
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
using hihapi.Exceptions;

namespace hihapi.Controllers
{
    public sealed class FinanceAccountsController : ODataController
    {
        private readonly hihDataContext _context;

        public FinanceAccountsController(hihDataContext context)
        {
            _context = context;
        }

        /// GET: /FinanceAccounts
        [EnableQuery]
        [Authorize]
        public IQueryable<FinanceAccount> Get([FromQuery]Int32 hid)
        {
            String usrName = String.Empty;
            try
            {
                usrName = HIHAPIUtility.GetUserID(this);
                if (String.IsNullOrEmpty(usrName))
                    throw new UnauthorizedAccessException();
            }
            catch
            {
                throw new UnauthorizedAccessException();
            }

            // Check whether User assigned with specified Home ID
            var hms = _context.HomeMembers.Where(p => p.HomeID == hid && p.User == usrName).Count();
            if (hms <= 0)
            {
                throw new UnauthorizedAccessException();
            }

            var rst = from acnt in _context.FinanceAccount.Where(p => p.HomeID == hid)
                select acnt;

            return rst;
        }

        [EnableQuery]
        [Authorize]
        public SingleResult<FinanceAccount> Get([FromODataUri]Int32 acntid, [FromODataUri]Int32 hid)
        {
            String usrName = String.Empty;
            try
            {
                usrName = HIHAPIUtility.GetUserID(this);
                if (String.IsNullOrEmpty(usrName))
                    throw new UnauthorizedAccessException();
            }
            catch
            {
                throw new UnauthorizedAccessException();
            }

            //// Check whether User assigned with specified Home ID
            //var hms = _context.HomeMembers.Where(p => p.HomeID == hid && p.User == usrName).Count();
            //if (hms <= 0)
            //{
            //    throw new UnauthorizedAccessException();
            //}

            return SingleResult.Create(_context.FinanceAccount.Where(p => p.HomeID == acntid && p.HomeID == hid));
        }


        [Authorize]
        public async Task<IActionResult> Post([FromBody]FinanceAccount account)
        {
            if (!ModelState.IsValid)
            {
                foreach (var value in ModelState.Values)
                {
                    foreach (var err in value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(err.Exception?.Message);
                    }
                }

                return BadRequest();
            }

            // User
            String usrName = String.Empty;
            try
            {
                usrName = HIHAPIUtility.GetUserID(this);
                if (String.IsNullOrEmpty(usrName))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            catch
            {
                throw new UnauthorizedAccessException();
            }

            // Check whether User assigned with specified Home ID
            var hms = _context.HomeMembers.Where(p => p.HomeID == account.HomeID && p.User == usrName).Count();
            if (hms <= 0)
            {
                throw new UnauthorizedAccessException();
            }

            if (!account.IsValid())
                return BadRequest();

            _context.FinanceAccount.Add(account);
            await _context.SaveChangesAsync();

            return Created(account);
        }

        [Authorize]
        public async Task<IActionResult> Put([FromODataUri] int key, [FromBody]FinanceAccount update)
        {
            if (!ModelState.IsValid)
            {
                foreach (var value in ModelState.Values)
                {
                    foreach (var err in value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(err.Exception?.Message);
                    }
                }

                return BadRequest();
            }
            if (key != update.ID)
            {
                return BadRequest();
            }

            // User
            String usrName = String.Empty;
            try
            {
                usrName = HIHAPIUtility.GetUserID(this);
                if (String.IsNullOrEmpty(usrName))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            catch
            {
                throw new UnauthorizedAccessException();
            }

            // Check whether User assigned with specified Home ID
            var hms = _context.HomeMembers.Where(p => p.HomeID == update.HomeID && p.User == usrName).Count();
            if (hms <= 0)
            {
                throw new UnauthorizedAccessException();
            }

            if (!update.IsValid())
                return BadRequest();

            _context.Entry(update).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException exp)
            {
                if (!_context.FinanceAccount.Any(p => p.ID == key))
                {
                    return NotFound();
                }
                else
                {
                    throw new DBOperationException(exp.Message);
                }
            }

            return Updated(update);
        }

        [Authorize]
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            var cc = await _context.FinanceAccount.FindAsync(key);
            if (cc == null)
            {
                return NotFound();
            }

            _context.FinanceAccount.Remove(cc);
            await _context.SaveChangesAsync();

            return StatusCode(204); // HttpStatusCode.NoContent
        }
    }
}