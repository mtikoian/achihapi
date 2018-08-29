using System;
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

namespace achihapi.Controllers
{
    [Produces("application/json")]
    [Route("api/FinanceLoanDocument")]
    public class FinanceLoanDocumentController : Controller
    {
        // GET: api/FinanceLoanDocument
        [HttpGet]
        [Authorize]
        public IActionResult Get([FromQuery]Int32 hid, Boolean skipPosted = true, DateTime? dtbgn = null, DateTime? dtend = null)
        {
            return Forbid();
        }

        // GET: api/FinanceLoanDocument/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id, [FromQuery]Int32 hid = 0)
        {
            if (hid <= 0)
                return BadRequest("Not HID inputted");

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

            FinanceLoanDocumentUIViewModel vm = new FinanceLoanDocumentUIViewModel();

            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Boolean bError = false;
            String strErrMsg = "";
            Boolean bNotFound = false;

            try
            {
                queryString = HIHDBUtility.GetFinanceDocLoanQueryString(id, hid);

                await conn.OpenAsync();

                // Check Home assignment with current user
                try
                {
                    HIHAPIUtility.CheckHIDAssignment(conn, hid, usrName);
                }
                catch (Exception exp)
                {
                    return BadRequest(exp.Message);
                }

                SqlCommand cmd = new SqlCommand(queryString, conn);
                SqlDataReader reader = cmd.ExecuteReader();

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

                // Account
                while (reader.Read())
                {
                    FinanceAccountUIViewModel vmAccount = new FinanceAccountUIViewModel();
                    Int32 aidx = 0;
                    aidx = HIHDBUtility.FinAccountHeader_DB2VM(reader, vmAccount, aidx);
                    vmAccount.ExtraInfo_Loan = new FinanceAccountExtLoanViewModel();
                    HIHDBUtility.FinAccountLoan_DB2VM(reader, vmAccount.ExtraInfo_Loan, aidx);

                    vm.AccountVM = vmAccount;
                }
                reader.NextResult();

                // Tmp docs
                while (reader.Read())
                {
                    FinanceTmpDocLoanViewModel loanvm = new FinanceTmpDocLoanViewModel();
                    HIHDBUtility.FinTmpDocLoan_DB2VM(reader, loanvm);
                    vm.AccountVM.ExtraInfo_Loan.LoanTmpDocs.Add(loanvm);
                }
                reader.NextResult();

                // Tag
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
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                }
            }

            if (bNotFound)
            {
                return NotFound();
            }
            else if (bError)
            {
                return StatusCode(500, strErrMsg);
            }

            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            return new JsonResult(vm, setting);
        }

        // POST: api/FinanceLoanDocument
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody]FinanceLoanDocumentUIViewModel vm)
        {
            if (vm == null || (vm.DocType != FinanceDocTypeViewModel.DocType_BorrowFrom
                && vm.DocType != FinanceDocTypeViewModel.DocType_LendTo))
            {
                return BadRequest("No data is inputted");
            }
            if (vm.HID <= 0)
                return BadRequest("Not HID inputted");

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

            // Check the items
            if (vm.Items.Count != 1 || vm.AccountVM.ExtraInfo_ADP.DPTmpDocs.Count <= 0)
            {
                return BadRequest("Only two items allowed or no template docs");
            }
            if (vm.AccountVM == null || vm.AccountVM.ExtraInfo_Loan == null)
            {
                return BadRequest("No account info!");
            }

            // Update the database
            SqlConnection conn = new SqlConnection(Startup.DBConnectionString);
            String queryString = "";
            Int32 nNewDocID = -1;
            Boolean bError = false;
            String strErrMsg = "";

            try
            {
                await conn.OpenAsync();

                // Check Home assignment with current user
                try
                {
                    HIHAPIUtility.CheckHIDAssignment(conn, vm.HID, usrName);
                }
                catch (Exception exp)
                {
                    return BadRequest(exp.Message);
                }

                SqlTransaction tran = conn.BeginTransaction();

                SqlCommand cmd = null;

                try
                {
                    // First, create the doc header => nNewDocID
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
                        if (vm.DocType == FinanceDocTypeViewModel.DocType_BorrowFrom)
                            ivm.TranType = FinanceTranTypeViewModel.TranType_BorrowFrom;
                        else if (vm.DocType == FinanceDocTypeViewModel.DocType_LendTo)
                            ivm.TranType = FinanceTranTypeViewModel.TranType_LendTo;

                        queryString = HIHDBUtility.GetFinDocItemInsertString();
                        SqlCommand cmd2 = new SqlCommand(queryString, conn)
                        {
                            Transaction = tran
                        };
                        HIHDBUtility.BindFinDocItemInsertParameter(cmd2, ivm, nNewDocID);

                        await cmd2.ExecuteNonQueryAsync();

                        cmd2.Dispose();
                        cmd2 = null;

                        // Tags
                        if (ivm.TagTerms.Count > 0)
                        {
                            // Create tags
                            foreach (var term in ivm.TagTerms)
                            {
                                queryString = HIHDBUtility.GetTagInsertString();

                                cmd2 = new SqlCommand(queryString, conn, tran);

                                HIHDBUtility.BindTagInsertParameter(cmd2, vm.HID, HIHTagTypeEnum.FinanceDocumentItem, nNewDocID, term, ivm.ItemID);

                                await cmd2.ExecuteNonQueryAsync();

                                cmd2.Dispose();
                                cmd2 = null;
                            }
                        }
                    }

                    // Third, go to the account creation => nNewAccountID
                    queryString = HIHDBUtility.GetFinanceAccountHeaderInsertString();

                    cmd = new SqlCommand(queryString, conn)
                    {
                        Transaction = tran
                    };
                    HIHDBUtility.BindFinAccountInsertParameter(cmd, vm.AccountVM, usrName);

                    SqlParameter idparam2 = cmd.Parameters.AddWithValue("@Identity", SqlDbType.Int);
                    idparam2.Direction = ParameterDirection.Output;

                    nRst = await cmd.ExecuteNonQueryAsync();
                    Int32 nNewAccountID = (Int32)idparam2.Value;
                    cmd.Dispose();
                    cmd = null;

                    // 3a. Create another item to loan document
                    var nMaxItemID = vm.Items.Max(item => item.ItemID);
                    foreach (FinanceDocumentItemUIViewModel ivm in vm.Items)
                    {
                        ivm.ItemID = ++nMaxItemID;
                        ivm.AccountID = nNewAccountID;
                        if (vm.DocType == FinanceDocTypeViewModel.DocType_BorrowFrom)
                            ivm.TranType = FinanceTranTypeViewModel.TranType_OpeningLiability;
                        else if (vm.DocType == FinanceDocTypeViewModel.DocType_LendTo)
                            ivm.TranType = FinanceTranTypeViewModel.TranType_OpeningAsset;

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

                    // Fourth, creat the Loan part
                    queryString = HIHDBUtility.GetFinanceAccountLoanInsertString();
                    cmd = new SqlCommand(queryString, conn)
                    {
                        Transaction = tran
                    };

                    HIHDBUtility.BindFinAccountLoanInsertParameter(cmd, vm.AccountVM.ExtraInfo_Loan, nNewDocID, nNewAccountID, usrName);
                    nRst = await cmd.ExecuteNonQueryAsync();
                    cmd.Dispose();
                    cmd = null;

                    // Fifth, create template docs
                    foreach (FinanceTmpDocLoanViewModel avm in vm.AccountVM.ExtraInfo_Loan.LoanTmpDocs)
                    {
                        queryString = HIHDBUtility.GetFinanceTmpDocLoanInsertString();

                        cmd = new SqlCommand(queryString, conn)
                        {
                            Transaction = tran
                        };

                        HIHDBUtility.BindFinTmpDocLoanParameter(cmd, avm, nNewAccountID, usrName);
                        await cmd.ExecuteNonQueryAsync();

                        cmd.Dispose();
                        cmd = null;
                    }

                    tran.Commit();
                }
                catch (Exception exp)
                {
                    if (tran != null)
                        tran.Rollback();

                    throw exp; // Re-throw
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                bError = true;
                strErrMsg = exp.Message;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                }
            }

            if (bError)
                return StatusCode(500, strErrMsg);

            vm.ID = nNewDocID;
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                DateFormatString = HIHAPIConstants.DateFormatPattern,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            return new JsonResult(vm, setting);
        }

        // PUT: api/FinanceLoanDocument/5
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Put(int id, [FromBody]string value)
        {
            return BadRequest();
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            return BadRequest();
        }
    }
}
