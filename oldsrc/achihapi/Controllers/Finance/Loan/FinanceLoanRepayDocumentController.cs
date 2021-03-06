﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using achihapi.ViewModels;
using System.Data;
using System.Data.SqlClient;
using achihapi.Utilities;
using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace achihapi.Controllers
{
    [Produces("application/json")]
    [Route("api/FinanceLoanRepayDocument")]
    public class FinanceLoanRepayDocumentController : Controller
    {
        private IMemoryCache _cache;
        public FinanceLoanRepayDocumentController(IMemoryCache cache)
        {
            _cache = cache;
        }

        // GET: api/FinanceLoanRepayDocument
        [HttpGet]
        public IActionResult Get()
        {
            return Forbid();
        }

        // GET: api/FinanceLoanRepayDocument/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Forbid();
        }
        
        // POST: api/FinanceLoanRepayDocument
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromQuery]Int32 hid, Int32 loanAccountID, Int32? tmpdocid, [FromBody]FinanceDocumentUIViewModel repaydoc)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // The post here is:
            // 1. Post a repayment document with the content from this template doc
            // 2. Update the template doc with REFDOCID
            // 3. If the account balance is zero, close the account;

            // Basic check
            if (hid <= 0 || (tmpdocid.HasValue && tmpdocid.Value <= 0)
                || loanAccountID <= 0
                || repaydoc == null || repaydoc.HID != hid
                || repaydoc.DocType != FinanceDocTypeViewModel.DocType_Repay)
            {
                return BadRequest("No data inputted!");
            }
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader reader = null;
            SqlTransaction tran = null;
            String queryString = String.Empty;
            String strErrMsg = String.Empty;
            HttpStatusCode errorCode = HttpStatusCode.OK;
            Decimal acntBalance = 0M;

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
            FinanceTmpDocLoanViewModel vmTmpDoc = new FinanceTmpDocLoanViewModel();
            HomeDefViewModel vmHome = new HomeDefViewModel();
            FinanceAccountUIViewModel vmAccount = new FinanceAccountUIViewModel();

            try
            {
                using(conn = new SqlConnection(Startup.DBConnectionString))
                {
                    await conn.OpenAsync();

                    // Check: HID, it requires more info than just check, so it implemented it 
                    try
                    {
                        HIHAPIUtility.CheckHIDAssignment(conn, hid, usrName);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    // Check: DocID
                    String checkString = "";
                    if (tmpdocid.HasValue)
                    {
                        checkString = HIHDBUtility.GetFinanceDocLoanListQueryString() + " WHERE [DOCID] = " + tmpdocid.Value.ToString() + " AND [HID] = " + hid.ToString();
                        cmd = new SqlCommand(checkString, conn);
                        reader = cmd.ExecuteReader();

                        if (!reader.HasRows)
                        {
                            errorCode = HttpStatusCode.BadRequest;
                            throw new Exception("Invalid Doc ID inputted: " + tmpdocid.Value.ToString());
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                HIHDBUtility.FinTmpDocLoan_DB2VM(reader, vmTmpDoc);

                                // It shall be only one entry if found!
                                break;
                            }
                        }

                        reader.Dispose();
                        reader = null;
                        cmd.Dispose();
                        cmd = null;
                    }

                    // Check: Tmp doc has posted or not?
                    if (vmTmpDoc == null || (vmTmpDoc.RefDocID.HasValue && vmTmpDoc.RefDocID.Value > 0)
                        || vmTmpDoc.AccountID != loanAccountID)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw new Exception("Tmp Doc not existed yet or has been posted");
                    }

                    // Check: Loan account
                    checkString = HIHDBUtility.GetFinanceLoanAccountQueryString(hid, loanAccountID);
                    cmd = new SqlCommand(checkString, conn);
                    reader = cmd.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw new Exception("Loan account read failed based on Doc ID inputted: " + tmpdocid.ToString());
                    }
                    else
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                HIHDBUtility.FinAccountHeader_DB2VM(reader, vmAccount, 0);
                                break;
                            }
                        }
                        reader.NextResult();

                        vmAccount.ExtraInfo_Loan = new FinanceAccountExtLoanViewModel();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                HIHDBUtility.FinAccountLoan_DB2VM(reader, vmAccount.ExtraInfo_Loan, 0);
                                break;
                            }
                        }
                        reader.NextResult();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    acntBalance = reader.GetDecimal(0);
                                }
                                break;
                            }
                        }
                    }
                    reader.Dispose();
                    reader = null;
                    cmd.Dispose();
                    cmd = null;

                    // Data validation - basic
                    try
                    {
                        await FinanceDocumentController.FinanceDocumentBasicValidationAsync(repaydoc, conn);
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    // Data validation - loan specific
                    try
                    {
                        int ninvaliditems = 0;
                        // Only four tran. types are allowed
                        if (vmAccount.CtgyID == FinanceAccountCtgyViewModel.AccountCategory_BorrowFrom)
                        {
                            ninvaliditems = repaydoc.Items.Where(item => item.TranType != FinanceTranTypeViewModel.TranType_InterestOut
                                && item.TranType != FinanceTranTypeViewModel.TranType_RepaymentOut
                                && item.TranType != FinanceTranTypeViewModel.TranType_RepaymentIn)
                                .Count();
                        }
                        else if (vmAccount.CtgyID == FinanceAccountCtgyViewModel.AccountCategory_LendTo)
                        {
                            ninvaliditems = repaydoc.Items.Where(item => item.TranType != FinanceTranTypeViewModel.TranType_InterestIn
                                && item.TranType != FinanceTranTypeViewModel.TranType_RepaymentOut
                                && item.TranType != FinanceTranTypeViewModel.TranType_RepaymentIn)
                                .Count();
                        }
                        if (ninvaliditems > 0)
                        {
                            throw new Exception("Items with invalid tran type");
                        }

                        // Check the amount
                        decimal totalOut = repaydoc.Items.Where(item => item.TranType == FinanceTranTypeViewModel.TranType_RepaymentOut).Sum(item2 => item2.TranAmount);
                        decimal totalIn = repaydoc.Items.Where(item => item.TranType == FinanceTranTypeViewModel.TranType_RepaymentIn).Sum(item2 => item2.TranAmount);
                        //decimal totalintOut = repaydoc.Items.Where(item => (item.TranType == FinanceTranTypeViewModel.TranType_InterestOut)).Sum(item2 => item2.TranAmount);

                        // New account balance
                        if (vmAccount.CtgyID == FinanceAccountCtgyViewModel.AccountCategory_BorrowFrom)
                        {
                            acntBalance += totalOut;
                        }
                        else if (vmAccount.CtgyID == FinanceAccountCtgyViewModel.AccountCategory_LendTo)
                        {
                            acntBalance -= totalIn;
                        }
                        if (totalOut != totalIn)
                        {
                            throw new Exception("Amount is not equal!");
                        }
                    }
                    catch (Exception)
                    {
                        errorCode = HttpStatusCode.BadRequest;
                        throw;
                    }

                    // Now go ahead for the creating
                    tran = conn.BeginTransaction();
                    Int32 nNewDocID = 0;

                    // Now go ahead for creating
                    queryString = HIHDBUtility.GetFinDocHeaderInsertString();

                    // Header
                    cmd = new SqlCommand(queryString, conn)
                    {
                        Transaction = tran
                    };

                    HIHDBUtility.BindFinDocHeaderInsertParameter(cmd, repaydoc, usrName);
                    SqlParameter idparam = cmd.Parameters.AddWithValue("@Identity", SqlDbType.Int);
                    idparam.Direction = ParameterDirection.Output;

                    Int32 nRst = await cmd.ExecuteNonQueryAsync();
                    nNewDocID = (Int32)idparam.Value;
                    repaydoc.ID = nNewDocID;
                    cmd.Dispose();
                    cmd = null;

                    // Then, creating the items
                    foreach (FinanceDocumentItemUIViewModel ivm in repaydoc.Items)
                    {
                        queryString = HIHDBUtility.GetFinDocItemInsertString();

                        SqlCommand cmd2 = new SqlCommand(queryString, conn)
                        {
                            Transaction = tran
                        };
                        HIHDBUtility.BindFinDocItemInsertParameter(cmd2, ivm, nNewDocID);

                        await cmd2.ExecuteNonQueryAsync();
                        cmd2.Dispose();
                        cmd2 = null;
                    }

                    // Then, update the template doc
                    queryString = @"UPDATE [dbo].[t_fin_tmpdoc_loan]
                                       SET [REFDOCID] = @REFDOCID
                                          ,[UPDATEDBY] = @UPDATEDBY
                                          ,[UPDATEDAT] = @UPDATEDAT
                                     WHERE [HID] = @HID AND [DOCID] = @DOCID";
                    SqlCommand cmdTmpDoc = new SqlCommand(queryString, conn)
                    {
                        Transaction = tran
                    };
                    cmdTmpDoc.Parameters.AddWithValue("@REFDOCID", nNewDocID);
                    cmdTmpDoc.Parameters.AddWithValue("@UPDATEDBY", usrName);
                    cmdTmpDoc.Parameters.AddWithValue("@UPDATEDAT", DateTime.Now);
                    cmdTmpDoc.Parameters.AddWithValue("@HID", hid);
                    cmdTmpDoc.Parameters.AddWithValue("@DOCID", tmpdocid);
                    await cmdTmpDoc.ExecuteNonQueryAsync();
                    cmdTmpDoc.Dispose();
                    cmdTmpDoc = null;

                    // Incase balance is zero, update the account status
                    if (Decimal.Compare(acntBalance, 0) == 0)
                    {
                        queryString = HIHDBUtility.GetFinanceAccountStatusUpdateString();
                        SqlCommand cmdAccount = new SqlCommand(queryString, conn, tran);
                        HIHDBUtility.BindFinAccountStatusUpdateParameter(cmdAccount, FinanceAccountStatus.Closed, loanAccountID, hid, usrName);
                        await cmdAccount.ExecuteNonQueryAsync();
                        cmdAccount.Dispose();
                        cmdAccount = null;
                    }

                    tran.Commit();

                    // Update the buffer of the relevant Account!
                    // Account List
                    try
                    {
                        var cacheKey = String.Format(CacheKeys.FinAccountList, hid, null);
                        this._cache.Remove(cacheKey);

                        cacheKey = String.Format(CacheKeys.FinAccount, hid, loanAccountID);
                        this._cache.Remove(cacheKey);
                    }
                    catch (Exception)
                    {
                        // Do nothing here.
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

            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            return new JsonResult(repaydoc, setting);
        }

        // PUT: api/FinanceLoanRepayDocument/5
        [HttpPut("{id}")]
        public IActionResult Put([FromRoute]int id, [FromBody]string value)
        {
            return Forbid();
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public IActionResult Delete([FromRoute]int id)
        {
            return Forbid();
        }
    }
}
