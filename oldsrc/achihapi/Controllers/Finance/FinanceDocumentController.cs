﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using achihapi.ViewModels;
using System.Data;
using System.Data.SqlClient;
using achihapi.Utilities;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Reflection;

namespace achihapi.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class FinanceDocumentController : Controller
    {
        private IMemoryCache _cache;
        public FinanceDocumentController(IMemoryCache cache)
        {
            _cache = cache;
        }

        // GET: api/financedocument
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get([FromQuery]Int32 hid, DateTime? dtbgn = null, DateTime? dtend = null, Int32 top = 100, Int32 skip = 0)
        {
            BaseListViewModel<FinanceDocumentUIViewModel> listVMs = new BaseListViewModel<FinanceDocumentUIViewModel>();
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader reader = null;
            String queryString = "";
            String strErrMsg = "";
            HttpStatusCode errorCode = HttpStatusCode.OK;

            if (hid <= 0)
                return BadRequest("No Home Inputted");
            String usrName = String.Empty;
            if (Startup.UnitTestMode)
                usrName = UnitTestUtility.UnitTestUser;
            else
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
            }
            if (String.IsNullOrEmpty(usrName))
                return BadRequest("User cannot recognize");

            try
            {
                using (conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Check Home assignment with current user
                    try
                    {
                        HIHAPIUtility.CheckHIDAssignment(conn, hid, usrName);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    queryString = @"SELECT count(*) FROM[dbo].[t_fin_document] WHERE[HID] = @hid ";
                    if (dtbgn.HasValue)
                        queryString += " AND [TRANDATE] >= @dtbgn ";
                    if (dtend.HasValue)
                        queryString += " AND [TRANDATE] <= @dtend ";
                    cmd = new SqlCommand(queryString, conn);
                    cmd.Parameters.AddWithValue("@hid", hid);
                    if (dtbgn.HasValue)
                        cmd.Parameters.AddWithValue("@dtbgn", dtbgn.Value);
                    if (dtend.HasValue)
                        cmd.Parameters.AddWithValue("@dtend", dtend.Value);
                    reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        listVMs.TotalCount = reader.GetInt32(0);
                        break;
                    }
                    reader.Dispose();
                    reader = null;
                    cmd.Dispose();
                    cmd = null;

                    if (listVMs.TotalCount > 0)
                    {
                        queryString = HIHDBUtility.getFinanceDocListQueryString();
                        queryString += @" WHERE [HID] = @hid ";
                        if (dtbgn.HasValue)
                            queryString += " AND [TRANDATE] >= @dtbgn ";
                        if (dtend.HasValue)
                            queryString += " AND [TRANDATE] <= @dtend ";
                        queryString += " ORDER BY [TRANDATE] DESC OFFSET " + skip.ToString() + " ROWS FETCH NEXT " + top.ToString() + " ROWS ONLY;";
                        cmd = new SqlCommand(queryString, conn);
                        cmd.Parameters.AddWithValue("@hid", hid);
                        if (dtbgn.HasValue)
                            cmd.Parameters.AddWithValue("@dtbgn", dtbgn.Value);
                        if (dtend.HasValue)
                            cmd.Parameters.AddWithValue("@dtend", dtend.Value);
                        reader = await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                FinanceDocumentUIViewModel avm = new FinanceDocumentUIViewModel();
                                HIHDBUtility.FinDocList_DB2VM(reader, avm);

                                listVMs.Add(avm);
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                strErrMsg = exp.Message;
                if (errorCode == HttpStatusCode.OK)
                    errorCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (errorCode != HttpStatusCode.OK)
            {
                switch (errorCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    case HttpStatusCode.NotFound:
                        return NotFound();
                    case HttpStatusCode.BadRequest:
                        return BadRequest(strErrMsg);
                    default:
                        return StatusCode(500, strErrMsg);
                }
            }

            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            ;
            return new JsonResult(listVMs, setting);
        }

        // GET api/financedocument/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get([FromRoute]int id, [FromQuery]Int32 hid = 0)
        {
            if (hid <= 0)
                return BadRequest("No Home Inputted");

            String usrName = String.Empty;
            if (Startup.UnitTestMode)
                usrName = UnitTestUtility.UnitTestUser;
            else
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
            }
            if (String.IsNullOrEmpty(usrName))
                return BadRequest("User cannot recognize");

            FinanceDocumentUIViewModel vm = new FinanceDocumentUIViewModel();

            SqlConnection conn = null;
            String queryString = "";
            String strErrMsg = "";
            HttpStatusCode errorCode = HttpStatusCode.OK;

            try
            {
                queryString = HIHDBUtility.getFinanceDocQueryString(id, hid);

                using (conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Check Home assignment with current user
                    try
                    {
                        HIHAPIUtility.CheckHIDAssignment(conn, hid, usrName);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    // Read the doc out
                    try
                    {
                        ReadFinanceDocument(id, hid, conn, vm);
                    }
                    catch(Exception)
                    {
                        errorCode = HttpStatusCode.NotFound;

                        throw;
                    }
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                strErrMsg = exp.Message;
                if (errorCode == HttpStatusCode.OK)
                    errorCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (errorCode != HttpStatusCode.OK)
            {
                switch (errorCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    case HttpStatusCode.NotFound:
                        return NotFound();
                    case HttpStatusCode.BadRequest:
                        return BadRequest(strErrMsg);
                    default:
                        return StatusCode(500, strErrMsg);
                }
            }

            return new ObjectResult(vm);
        }

        // POST api/financedocument
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody]FinanceDocumentUIViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (vm == null || vm.DocType == FinanceDocTypeViewModel.DocType_AdvancePayment
                || vm.DocType == FinanceDocTypeViewModel.DocType_AdvanceReceive
                || vm.DocType == FinanceDocTypeViewModel.DocType_AssetBuyIn
                || vm.DocType == FinanceDocTypeViewModel.DocType_AssetSoldOut
                || vm.DocType == FinanceDocTypeViewModel.DocType_AssetValChg
                || vm.DocType == FinanceDocTypeViewModel.DocType_BorrowFrom
                || vm.DocType == FinanceDocTypeViewModel.DocType_LendTo)
            {
                return BadRequest("No data is inputted or not supported Advancepay/Loan/Asset");
            }
            if (vm.HID <= 0)
                return BadRequest("No Home ID inputted");

            // Do the basic check!
            try
            {
                FinanceDocumentBasicCheck(vm);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.Message);
            }

            // Update the database
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader reader = null;
            SqlTransaction tran = null;
            String queryString = "";
            Int32 nNewDocID = -1;
            String strErrMsg = "";
            HttpStatusCode errorCode = HttpStatusCode.OK;

            String usrName = String.Empty;
            if (Startup.UnitTestMode)
                usrName = UnitTestUtility.UnitTestUser;
            else
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
            }
            if (String.IsNullOrEmpty(usrName))
                return BadRequest("User cannot recognize");

            try
            {
                using (conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Do the validation
                    try
                    {
                        await FinanceDocumentBasicValidationAsync(vm, conn);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    tran = conn.BeginTransaction();

                    // Now go ahead for the creating
                    queryString = HIHDBUtility.GetFinDocHeaderInsertString();

                    cmd = new SqlCommand(queryString, conn)
                    {
                        Transaction = tran
                    };

                    HIHDBUtility.BindFinDocHeaderInsertParameter(cmd, vm, usrName);
                    SqlParameter idparam = cmd.Parameters.AddWithValue("@Identity", SqlDbType.Int);
                    idparam.Direction = ParameterDirection.Output;

                    Int32 nRst = await cmd.ExecuteNonQueryAsync();
                    nNewDocID = (Int32)idparam.Value;
                    cmd.Dispose();
                    cmd = null;

                    // Then, creating the items
                    foreach (FinanceDocumentItemUIViewModel ivm in vm.Items)
                    {
                        queryString = HIHDBUtility.GetFinDocItemInsertString();
                        cmd = new SqlCommand(queryString, conn)
                        {
                            Transaction = tran
                        };
                        HIHDBUtility.BindFinDocItemInsertParameter(cmd, ivm, nNewDocID);

                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Tags
                        if (ivm.TagTerms.Count > 0)
                        {
                            // Create tags
                            foreach (var term in ivm.TagTerms)
                            {
                                queryString = HIHDBUtility.GetTagInsertString();

                                cmd = new SqlCommand(queryString, conn, tran);

                                HIHDBUtility.BindTagInsertParameter(cmd, vm.HID, HIHTagTypeEnum.FinanceDocumentItem, nNewDocID, term, ivm.ItemID);

                                await cmd.ExecuteNonQueryAsync();

                                cmd.Dispose();
                                cmd = null;
                            }
                        }
                    }

                    tran.Commit();
                }
            }
            catch (Exception exp)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exp.Message);
#endif

                if (tran != null)
                    tran.Rollback();

                strErrMsg = exp.Message;
                if (errorCode == HttpStatusCode.OK)
                    errorCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (errorCode != HttpStatusCode.OK)
            {
                switch (errorCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    case HttpStatusCode.NotFound:
                        return NotFound();
                    case HttpStatusCode.BadRequest:
                        return BadRequest(strErrMsg);
                    default:
                        return StatusCode(500, strErrMsg);
                }
            }

            vm.ID = nNewDocID;
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            return new JsonResult(vm, setting);
        }

        // PUT api/financedocument/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put([FromRoute]int id, [FromBody]FinanceDocumentUIViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // For document update, it accepts all documents now.
            if (vm == null)
            {
                return BadRequest("No data is inputted or for Advancepay/Loan/Asset");
            }
            if (vm.HID <= 0)
                return BadRequest("No Home ID inputted");
            if (vm.ID <= 0)
                return BadRequest("No Doc ID inputted");

            // Do the basic check!
            try
            {
                FinanceDocumentBasicCheck(vm);
            }
            catch(Exception exp)
            {
                return BadRequest(exp.Message);
            }

            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader reader = null;
            SqlTransaction tran = null;
            String strErrMsg = "";
            HttpStatusCode errorCode = HttpStatusCode.OK;

            String usrName = String.Empty;
            if (Startup.UnitTestMode)
                usrName = UnitTestUtility.UnitTestUser;
            else
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
            }
            if (String.IsNullOrEmpty(usrName))
                return BadRequest("User cannot recognize");

            try
            {
                using (conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Do the validation
                    try
                    {
                        await FinanceDocumentBasicValidationAsync(vm, conn);
                    }
                    catch (Exception exp)
                    {
                        return BadRequest(exp.Message);
                    }

                    // Old doc
                    var vmOld = new FinanceDocumentUIViewModel();
                    try
                    {
                        ReadFinanceDocument(id, vm.HID, conn, vmOld);
                    }
                    catch(Exception exp)
                    {
                        return BadRequest(exp.Message);
                    }

                    // Perform the check!
                    if (vmOld.ID != vm.ID || vmOld.HID != vm.HID || vmOld.DocType != vm.DocType)
                    {
                        return BadRequest("Doc ID/HID/Doc Type mismatched");
                    }
                    else
                    {
                        var diffHeader = FinanceDocumentUIViewModel.WorkoutDeltaForHeaderUpdate(vmOld, vm, usrName);
                        switch (vmOld.DocType)
                        {
                            case FinanceDocTypeViewModel.DocType_Normal:
                                break;

                            default:
                                {
                                    var keyidx = diffHeader.Keys.ToList().FindIndex(key =>
                                    {
                                        if (String.CompareOrdinal(key, "Desp") != 0
                                            && String.CompareOrdinal(key, "UpdatedBy") != 0
                                            && String.CompareOrdinal(key, "UpdatedAt") != 0)
                                        {
                                            return true;
                                        }
                                        return false;
                                    });
                                    if (keyidx != -1)
                                    {
                                        return BadRequest("No supported fields in header found");
                                    }

                                    var diffItems = FinanceDocumentUIViewModel.WorkoutDeltaForItemUpdate(vmOld, vm);
                                    if (diffItems.Count > 0)
                                    {
                                        foreach(var ditem in diffItems)
                                        {
                                            if (ditem.Value == null)
                                            {
                                                return BadRequest("No supported line item operation");
                                            }
                                            else if (ditem.Value is FinanceDocumentItemUIViewModel)
                                            {
                                                return BadRequest("No supported line item operation");
                                            }
                                            else
                                            {
                                                var diffitem = ditem.Value as Dictionary<String, Object>;
                                                Dictionary<String, Object> dictAllowed = new Dictionary<string, object>();
                                                dictAllowed.Add("Desp", null);
                                                dictAllowed.Add("ControlCenterID", null);
                                                dictAllowed.Add("OrderID", null);
                                                dictAllowed.Add("TagTerm", null);
                                                foreach(var dkey in diffitem.Keys)
                                                {
                                                    if (!dictAllowed.ContainsKey(dkey))
                                                    {
                                                        return BadRequest("Non supported field in line item found");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }

                    }

                    // Workout the delta
                    var headerSql = FinanceDocumentUIViewModel.WorkoutDeltaForHeaderUpdateSqlString(vmOld, vm, usrName);
                    var itemSqls = FinanceDocumentUIViewModel.WorkoutDeltaForItemUpdateSqlString(vmOld, vm);

                    // Start the DB update
                    tran = conn.BeginTransaction();

                    // Now go ahead for the header updating
                    if (!String.IsNullOrEmpty(headerSql))
                    {
                        cmd = new SqlCommand(headerSql, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;
                    }

                    // Then line tems updating
                    foreach (var isql in itemSqls)
                    {
                        if (!String.IsNullOrEmpty(isql))
                        {
                            cmd = new SqlCommand(isql, conn, tran);
                            await cmd.ExecuteNonQueryAsync();
                            cmd.Dispose();
                            cmd = null;
                        }
                    }

                    // Commit it
                    tran.Commit();
                }
            }
            catch (Exception exp)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exp.Message);
#endif

                if (tran != null)
                    tran.Rollback();

                strErrMsg = exp.Message;
                if (errorCode == HttpStatusCode.OK)
                    errorCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (errorCode != HttpStatusCode.OK)
            {
                switch (errorCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    case HttpStatusCode.NotFound:
                        return NotFound();
                    case HttpStatusCode.BadRequest:
                        return BadRequest(strErrMsg);
                    default:
                        return StatusCode(500, strErrMsg);
                }
            }

            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            return new JsonResult(vm, setting);
        }

        // DELETE api/financedocument/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute]int id, [FromQuery]Int32 hid = 0)
        {
            if (hid <= 0)
                return BadRequest("No Home Inputted");

            String usrName = String.Empty;
            if (Startup.UnitTestMode)
                usrName = UnitTestUtility.UnitTestUser;
            else
            {
                var usrObj = HIHAPIUtility.GetUserClaim(this);
                usrName = usrObj.Value;
            }
            if (String.IsNullOrEmpty(usrName))
                return BadRequest("User cannot recognize");

            // Update the database
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader reader = null;
            SqlTransaction tran = null;
            String queryString = "";
            Int32 accID = -1, tmpdocID = -1, relAccID = -1;
            FinanceAccountStatus relAccStatus = FinanceAccountStatus.Normal;
            List<Int32> listPostedID = new List<Int32>();
            HttpStatusCode errorCode = HttpStatusCode.OK;
            String strErrMsg = "";

            try
            {
                queryString = @"SELECT [ACCOUNTID],[REFDOCID] FROM [dbo].[t_fin_account_ext_dp] WHERE [REFDOCID] = " + id.ToString() 
                        + " UNION ALL SELECT [ACCOUNTID],[REFDOCID] FROM [dbo].[t_fin_account_ext_loan] WHERE [REFDOCID] = " + id.ToString()
                        + " UNION ALL SELECT [ACCOUNTID],[REFDOC_BUY] AS [REFDOCID] FROM [dbo].[t_fin_account_ext_as] WHERE [REFDOC_BUY] = " + id.ToString()
                        + " UNION ALL SELECT [ACCOUNTID],[REFDOC_SOLD] AS [REFDOCID] FROM [dbo].[t_fin_account_ext_as] WHERE [REFDOC_SOLD] = " + id.ToString()
                        ;

                using (conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Check Home assignment with current user
                    try
                    {
                        HIHAPIUtility.CheckHIDAssignment(conn, hid, usrName);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    // Check current document is used for creating account for adp
                    cmd = new SqlCommand(queryString, conn);
                    reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            accID = reader.GetInt32(0);
                            break; // One doc shall be reference to one Account at maximum
                        }
                    }
                    reader.Dispose();
                    reader = null;
                    cmd.Dispose();
                    cmd = null;

                    if (accID == -1)
                    {
                        // Check current document is referenced in template doc
                        queryString = @"SELECT [DOCID],[ACCOUNTID],[REFDOCID] FROM [dbo].[t_fin_tmpdoc_dp] WHERE [REFDOCID] = " + id.ToString()
                                + @" UNION ALL SELECT [DOCID],[ACCOUNTID],[REFDOCID] FROM [dbo].[t_fin_tmpdoc_loan] WHERE [REFDOCID] = " + id.ToString();

                        cmd = new SqlCommand(queryString, conn);
                        reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                tmpdocID = reader.GetInt32(0);
                                relAccID = reader.GetInt32(1);
                                break; // One doc shall be reference to one tmp. doc at maximum
                            }
                        }

                        reader.Dispose();
                        reader = null;
                        cmd.Dispose();
                        cmd = null;

                        if (tmpdocID != -1 && relAccID != -1)
                        {
                            queryString = @"SELECT [status] FROM [t_fin_account] WHERE [accountid] = " + relAccID.ToString();
                            cmd = new SqlCommand(queryString, conn);
                            reader = cmd.ExecuteReader();
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    relAccStatus = (FinanceAccountStatus)reader.GetByte(0);
                                    break; // One doc shall be reference to one tmp. doc at maximum
                                }
                            }

                            reader.Dispose();
                            reader = null;
                            cmd.Dispose();
                            cmd = null;
                        }
                    }
                    else
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        strErrMsg = "Document with account created cannot be deleted";
                        throw new Exception(strErrMsg);

                        // Need fetch out the document posted already
                        queryString = @"SELECT [REFDOCID] FROM [dbo].[t_fin_tmpdoc_dp] WHERE [ACCOUNTID] = " + accID.ToString()
                                + @" AND [REFDOCID] IS NOT NULL UNION ALL SELECT [REFDOCID] FROM [dbo].[t_fin_tmpdoc_loan] WHERE [ACCOUNTID] = " + accID.ToString()
                                + " AND [REFDOCID] IS NOT NULL";

                        cmd = new SqlCommand(queryString, conn);
                        reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                listPostedID.Add(reader.GetInt32(0));
                            }
                        }

                        reader.Dispose();
                        reader = null;
                        cmd.Dispose();
                        cmd = null;
                    }

                    // Checks have been applied, go ahead to deletion
                    tran = conn.BeginTransaction();

                    // Document header
                    queryString = @"DELETE FROM [dbo].[t_fin_document] WHERE [ID] = " + id.ToString();
                    cmd = new SqlCommand(queryString, conn, tran);
                    await cmd.ExecuteNonQueryAsync();
                    cmd.Dispose();
                    cmd = null;

                    // Document items
                    queryString = @"DELETE FROM [dbo].[t_fin_document_item] WHERE [DOCID] = " + id.ToString();
                    cmd = new SqlCommand(queryString, conn, tran);
                    await cmd.ExecuteNonQueryAsync();
                    cmd.Dispose();
                    cmd = null;

                    if (accID != -1)
                    {
                        // Account deletion
                        queryString = @"DELETE FROM [t_fin_account] WHERE [ID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // ADP account
                        queryString = @"DELETE FROM [dbo].[t_fin_account_ext_dp] WHERE [ACCOUNTID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Loan account
                        queryString = @"DELETE FROM [dbo].[t_fin_account_ext_loan] WHERE [ACCOUNTID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Asset account
                        queryString = @"DELETE FROM [dbo].[t_fin_account_ext_as] WHERE [ACCOUNTID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Template doc - ADP
                        queryString = @"DELETE FROM [dbo].[t_fin_tmpdoc_dp] WHERE [ACCOUNTID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Template doc - Loan
                        queryString = @"DELETE FROM [dbo].[t_fin_tmpdoc_loan] WHERE [ACCOUNTID] = " + accID.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Posted documents
                        foreach (Int32 npostid in listPostedID)
                        {
                            queryString = @"DELETE FROM [dbo].[t_fin_document] WHERE [ID] = " + npostid.ToString();
                            cmd = new SqlCommand(queryString, conn, tran);
                            await cmd.ExecuteNonQueryAsync();
                            cmd.Dispose();
                            cmd = null;

                            queryString = @"DELETE FROM [dbo].[t_fin_document_item] WHERE [DOCID] = " + npostid.ToString();
                            cmd = new SqlCommand(queryString, conn, tran);
                            await cmd.ExecuteNonQueryAsync();
                            cmd.Dispose();
                            cmd = null;
                        }
                    }
                    else if (tmpdocID != -1)
                    {
                        // Just clear the refernce id
                        queryString = @"UPDATE [dbo].[t_fin_tmpdoc_dp] SET [REFDOCID] = NULL WHERE [REFDOCID] = " + id.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Just clear the refernce id
                        queryString = @"UPDATE [dbo].[t_fin_tmpdoc_loan] SET [REFDOCID] = NULL WHERE [REFDOCID] = " + id.ToString();
                        cmd = new SqlCommand(queryString, conn, tran);
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Dispose();
                        cmd = null;

                        // Reset the account
                        if (relAccID != -1 && relAccStatus == FinanceAccountStatus.Closed)
                        {
                            queryString = HIHDBUtility.GetFinanceAccountStatusUpdateString();
                            SqlCommand cmdAccount = new SqlCommand(queryString, conn, tran);
                            HIHDBUtility.BindFinAccountStatusUpdateParameter(cmdAccount, FinanceAccountStatus.Normal, relAccID, hid, usrName);
                            await cmdAccount.ExecuteNonQueryAsync();
                            cmdAccount.Dispose();
                            cmdAccount = null;
                        }
                    }

                    tran.Commit();

                    if (relAccID != -1 && relAccStatus == FinanceAccountStatus.Closed)
                    {
                        try
                        {
                            var cacheKey = String.Format(CacheKeys.FinAccountList, hid, null);
                            this._cache.Remove(cacheKey);

                            cacheKey = String.Format(CacheKeys.FinAccount, hid, relAccID);
                            this._cache.Remove(cacheKey);
                        }
                        catch (Exception)
                        {
                            // Do nothing here.
                        }
                    }
                }
            }
            catch (Exception exp)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exp.Message);
#endif

                if (tran != null)
                    tran.Rollback();

                strErrMsg = exp.Message;
                if (errorCode == HttpStatusCode.OK)
                    errorCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (errorCode != HttpStatusCode.OK)
            {
                switch (errorCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    case HttpStatusCode.NotFound:
                        return NotFound();
                    case HttpStatusCode.BadRequest:
                        return BadRequest(strErrMsg);
                    default:
                        return StatusCode(500, strErrMsg);
                }
            }

            return Ok();
        }

        // Read the doc
        internal static void ReadFinanceDocument(Int32 docid, Int32 hid, SqlConnection conn, FinanceDocumentUIViewModel vm)
        {
            var queryString = HIHDBUtility.getFinanceDocQueryString(docid, hid);

            SqlCommand cmd = new SqlCommand(queryString, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            try
            {
                if (reader.HasRows)
                {
                    // Header
                    while (reader.Read())
                    {
                        HIHDBUtility.FinDocHeader_DB2VM(reader, vm);
                    }
                    reader.NextResult();

                    // Items
                    while (reader.Read())
                    {
                        FinanceDocumentItemUIViewModel itemvm = new FinanceDocumentItemUIViewModel();
                        HIHDBUtility.FinDocItem_DB2VM(reader, itemvm);

                        vm.Items.Add(itemvm);
                    }
                    reader.NextResult();

                    // Tags
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Int32 itemID = reader.GetInt32(0);
                            String sterm = reader.GetString(1);

                            foreach (var vitem in vm.Items)
                            {
                                if (vitem.ItemID == itemID)
                                {
                                    vitem.TagTerms.Add(sterm);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
            }
        }


        // Checks
        internal static void FinanceDocumentBasicCheck(FinanceDocumentUIViewModel vm)
        {
            // Header check!
            if (String.IsNullOrEmpty(vm.Desp))
                throw new Exception("No Desp in the header");
            if (vm.DocType == 0)
                throw new Exception("Doc type is must!");

            // Check the items
            if (vm.Items.Count <= 0)
                throw new Exception("No item has been assigned yet");

            foreach (var item in vm.Items)
            {
                if (item.AccountID == 0 || item.TranAmount == 0 || item.TranType == 0)
                    throw new Exception("Item must have account or tran. amount or tran. type"); ;
                if (item.ControlCenterID == 0 && item.OrderID == 0)
                    throw new Exception("Must input control object");
                if (item.ControlCenterID > 0 && item.OrderID > 0)
                    throw new Exception("Either control center or order shall be inputted, not both");
                if (String.IsNullOrEmpty(item.Desp))
                    throw new Exception("Desp is a must for an item");
            }
        }

        internal static async Task FinanceDocumentBasicValidationAsync(FinanceDocumentUIViewModel vm, SqlConnection conn, Int32? acntIDforSkipCheck = null)
        {
            // Then, go for other checks
            String strCheckString = @"SELECT TOP (1) [BASECURR] FROM [dbo].[t_homedef] WHERE [ID] = @hid;";
            SqlCommand cmdCheck = new SqlCommand(strCheckString, conn);
            cmdCheck.Parameters.AddWithValue("@hid", vm.HID);

            SqlDataReader reader = await cmdCheck.ExecuteReaderAsync();
            if (!reader.HasRows)
                throw new Exception("No home found");

            // Basic currency
            string basecurr = String.Empty;
            while (reader.Read())
            {
                basecurr = reader.GetString(0);
                break;
            }
            if (String.IsNullOrEmpty(basecurr))
                throw new Exception("No base currency defined!");

            reader.Dispose();
            reader = null;
            cmdCheck.Dispose();
            cmdCheck = null;

            // Currency
            strCheckString = @"SELECT TOP (1) CURR from t_fin_currency WHERE curr = @curr;";
            cmdCheck = new SqlCommand(strCheckString, conn);
            cmdCheck.Parameters.AddWithValue("@curr", vm.TranCurr);
            reader = await cmdCheck.ExecuteReaderAsync();
            if (!reader.HasRows)
                throw new Exception("No currency found");
            reader.Dispose();
            reader = null;
            cmdCheck.Dispose();
            cmdCheck = null;

            if (String.CompareOrdinal(vm.TranCurr, basecurr) != 0)
            {
                if (vm.ExgRate == 0)
                {
                    throw new Exception("No exchange rate info provided!");
                }
            }

            // Second currency
            if (!String.IsNullOrEmpty(vm.TranCurr2))
            {
                strCheckString = @"SELECT TOP (1) CURR from t_fin_currency WHERE curr = @curr;";
                cmdCheck = new SqlCommand(strCheckString, conn);
                cmdCheck.Parameters.AddWithValue("@curr", vm.TranCurr2);
                reader = await cmdCheck.ExecuteReaderAsync();
                if (!reader.HasRows)
                    throw new Exception("No currency found");

                reader.Dispose();
                reader = null;
                cmdCheck.Dispose();
                cmdCheck = null;

                if (String.CompareOrdinal(vm.TranCurr2, basecurr) != 0)
                {
                    if (vm.ExgRate2 == 0)
                    {
                        throw new Exception("No exchange rate info provided!");
                    }
                }
            }

            // Doc type
            strCheckString = @"SELECT TOP (1) [ID] FROM [t_fin_doc_type] WHERE [ID] = @ID"; // @"SELECT TOP (1) [ID] FROM [t_fin_doc_type] WHERE [HID] = @HID AND [ID] = @ID";
            cmdCheck = new SqlCommand(strCheckString, conn);
            //cmdCheck.Parameters.AddWithValue("@HID", vm.HID);
            cmdCheck.Parameters.AddWithValue("@ID", vm.DocType);
            reader = await cmdCheck.ExecuteReaderAsync();
            if (!reader.HasRows)
                throw new Exception("Invalid document type");
            reader.Dispose();
            reader = null;
            cmdCheck.Dispose();
            cmdCheck = null;

            Decimal totalamount = 0;
            foreach (var item in vm.Items)
            {
                // Account
                if (acntIDforSkipCheck.HasValue && acntIDforSkipCheck.Value == item.AccountID)
                {
                    // Do nothing
                }
                else
                {
                    strCheckString = @"SELECT TOP (1) [ID] FROM [t_fin_account] WHERE [HID] = @HID AND [ID] = @ID";
                    cmdCheck = new SqlCommand(strCheckString, conn);
                    cmdCheck.Parameters.AddWithValue("@HID", vm.HID);
                    cmdCheck.Parameters.AddWithValue("@ID", item.AccountID);
                    reader = await cmdCheck.ExecuteReaderAsync();
                    if (!reader.HasRows)
                        throw new Exception("No account found");
                    reader.Dispose();
                    reader = null;
                    cmdCheck.Dispose();
                    cmdCheck = null;
                }

                // Transaction type
                strCheckString = @"SELECT TOP (1) [ID], [EXPENSE] FROM [t_fin_tran_type] WHERE [ID] = @ID";//@"SELECT TOP (1) [ID], [EXPENSE] FROM [t_fin_tran_type] WHERE [HID] = @HID AND [ID] = @ID";
                cmdCheck = new SqlCommand(strCheckString, conn);
                //cmdCheck.Parameters.AddWithValue("@HID", vm.HID);
                cmdCheck.Parameters.AddWithValue("@ID", item.TranType);
                reader = await cmdCheck.ExecuteReaderAsync();
                if (!reader.HasRows)
                    throw new Exception("No tran. type found");

                Boolean isexp = false;
                while (reader.Read())
                {
                    isexp = reader.GetBoolean(1);
                    break;
                }
                reader.Dispose();
                reader = null;
                cmdCheck.Dispose();
                cmdCheck = null;

                // Control center
                if (item.ControlCenterID > 0)
                {
                    strCheckString = @"SELECT TOP (1) [ID] FROM [t_fin_controlcenter] WHERE [HID] = @HID AND [ID] = @ID";
                    cmdCheck = new SqlCommand(strCheckString, conn);
                    cmdCheck.Parameters.AddWithValue("@HID", vm.HID);
                    cmdCheck.Parameters.AddWithValue("@ID", item.ControlCenterID);
                    reader = await cmdCheck.ExecuteReaderAsync();
                    if (!reader.HasRows)
                        throw new Exception("No control center found");
                    reader.Dispose();
                    reader = null;
                    cmdCheck.Dispose();
                    cmdCheck = null;
                }

                // Order
                if (item.OrderID > 0)
                {
                    strCheckString = @"SELECT TOP (1) [ID] FROM [t_fin_order] WHERE [HID] = @HID AND [ID] = @ID AND (@VALIDDATE BETWEEN [VALID_FROM] AND [VALID_TO])";
                    cmdCheck = new SqlCommand(strCheckString, conn);
                    cmdCheck.Parameters.AddWithValue("@HID", vm.HID);
                    cmdCheck.Parameters.AddWithValue("@ID", item.OrderID);
                    cmdCheck.Parameters.AddWithValue("@VALIDDATE", vm.TranDate);
                    reader = await cmdCheck.ExecuteReaderAsync();
                    if (!reader.HasRows)
                        throw new Exception("No order found");
                    reader.Dispose();
                    reader = null;
                    cmdCheck.Dispose();
                    cmdCheck = null;
                }

                // Item amount
                Decimal itemAmt = 0;
                if (item.UseCurr2)
                {
                    if (isexp)
                    {
                        if (vm.ExgRate2 > 0)
                        {
                            itemAmt = -1 * item.TranAmount * vm.ExgRate2 / 100;
                        }
                        else
                        {
                            itemAmt = -1 * item.TranAmount;
                        }
                    }
                    else
                    {
                        if (vm.ExgRate2 > 0)
                        {
                            itemAmt = item.TranAmount * vm.ExgRate2 / 100;
                        }
                        else
                        {
                            itemAmt = item.TranAmount;
                        }
                    }
                }
                else
                {
                    if (isexp)
                    {
                        if (vm.ExgRate > 0)
                        {
                            itemAmt = -1 * item.TranAmount * vm.ExgRate / 100;
                        }
                        else
                        {
                            itemAmt = -1 * item.TranAmount;
                        }
                    }
                    else
                    {
                        if (vm.ExgRate > 0)
                        {
                            itemAmt = item.TranAmount * vm.ExgRate / 100;
                        }
                        else
                        {
                            itemAmt = item.TranAmount;
                        }
                    }
                }

                if (itemAmt == 0)
                {
                    throw new Exception("Amount is not correct");
                }

                totalamount += itemAmt;
            }

            if (vm.DocType == FinanceDocTypeViewModel.DocType_Transfer || vm.DocType == FinanceDocTypeViewModel.DocType_CurrExchange)
            {
                // Tolerance - 0.02
                if (Math.Abs(totalamount) > 0.02M)
                {
                    throw new Exception("Amount must be zero");
                }
            }
        }
    }
}
